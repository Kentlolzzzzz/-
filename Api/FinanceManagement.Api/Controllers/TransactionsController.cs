using FinanceManagement.Api.Data;
using FinanceManagement.Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinanceManagement.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Transactions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactions()
        {
            return await _context.Transactions
                .Include(t => t.Project)
                .Include(t => t.Account)
                .Include(t => t.IncomeCategory)
                .Include(t => t.ExpenseCategory)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        // GET: api/Transactions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Transaction>> GetTransaction(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Project)
                .Include(t => t.Account)
                .Include(t => t.IncomeCategory)
                .Include(t => t.ExpenseCategory)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                return NotFound();
            }

            return transaction;
        }

        // GET: api/Transactions/Project/5
        [HttpGet("Project/{projectId}")]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactionsByProject(int projectId)
        {
            return await _context.Transactions
                .Include(t => t.Project)
                .Include(t => t.Account)
                .Include(t => t.IncomeCategory)
                .Include(t => t.ExpenseCategory)
                .Where(t => t.ProjectId == projectId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        // POST: api/Transactions
        [HttpPost]
        public async Task<ActionResult<Transaction>> CreateTransaction(Transaction transaction)
        {
            // 设置创建信息
            transaction.CreatedAt = DateTime.Now;
            transaction.CreatedById = int.Parse(User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value);

            // 处理账户余额变更
            var account = await _context.Accounts.FindAsync(transaction.AccountId);
            if (account == null)
            {
                return BadRequest("账户不存在");
            }

            if (transaction.TransactionType == "Income")
            {
                account.Balance += transaction.Amount;
            }
            else if (transaction.TransactionType == "Expense")
            {
                account.Balance -= transaction.Amount;
            }

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
        }

        // PUT: api/Transactions/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransaction(int id, Transaction transaction)
        {
            if (id != transaction.Id)
            {
                return BadRequest();
            }

            // 获取原始交易记录以计算余额变化
            var originalTransaction = await _context.Transactions.FindAsync(id);
            if (originalTransaction == null)
            {
                return NotFound();
            }

            // 更新交易记录
            _context.Entry(originalTransaction).CurrentValues.SetValues(transaction);

            // 如果账户或金额变更，需更新账户余额
            if (originalTransaction.AccountId != transaction.AccountId ||
                originalTransaction.Amount != transaction.Amount ||
                originalTransaction.TransactionType != transaction.TransactionType)
            {
                // 恢复原账户余额
                var originalAccount = await _context.Accounts.FindAsync(originalTransaction.AccountId);
                if (originalAccount != null)
                {
                    if (originalTransaction.TransactionType == "Income")
                    {
                        originalAccount.Balance -= originalTransaction.Amount;
                    }
                    else if (originalTransaction.TransactionType == "Expense")
                    {
                        originalAccount.Balance += originalTransaction.Amount;
                    }
                }

                // 更新新账户余额
                var newAccount = await _context.Accounts.FindAsync(transaction.AccountId);
                if (newAccount != null)
                {
                    if (transaction.TransactionType == "Income")
                    {
                        newAccount.Balance += transaction.Amount;
                    }
                    else if (transaction.TransactionType == "Expense")
                    {
                        newAccount.Balance -= transaction.Amount;
                    }
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TransactionExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Transactions/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }

            // 恢复账户余额
            var account = await _context.Accounts.FindAsync(transaction.AccountId);
            if (account != null)
            {
                if (transaction.TransactionType == "Income")
                {
                    account.Balance -= transaction.Amount;
                }
                else if (transaction.TransactionType == "Expense")
                {
                    account.Balance += transaction.Amount;
                }
            }

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> TransactionExists(int id)
        {
            return await _context.Transactions.AnyAsync(e => e.Id == id);
        }

        // GET: api/Transactions/income-summary
        [HttpGet("income-summary")]
        public async Task<ActionResult<object>> GetIncomeSummary([FromQuery] int? year, [FromQuery] int? month)
        {
            var query = _context.Transactions.Where(t => t.TransactionType == "Income");

            // 应用时间筛选
            if (year.HasValue)
            {
                query = query.Where(t => t.TransactionDate.Year == year.Value);
                
                if (month.HasValue)
                {
                    query = query.Where(t => t.TransactionDate.Month == month.Value);
                }
            }

            var transactions = await query
                .Include(t => t.Project)
                .Include(t => t.IncomeCategory)
                .ToListAsync();

            // 计算总收入
            decimal totalIncome = transactions.Sum(t => t.Amount);

            // 按收入类别分组
            var categoryStats = transactions
                .GroupBy(t => t.IncomeCategory?.Name ?? "未分类")
                .Select(g => new
                {
                    Category = g.Key,
                    Amount = g.Sum(t => t.Amount),
                    Count = g.Count(),
                    Percentage = totalIncome > 0 ? Math.Round(g.Sum(t => t.Amount) / totalIncome * 100, 2) : 0
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            // 按项目分组
            var projectStats = transactions
                .GroupBy(t => t.Project?.Name ?? "未分配项目")
                .Select(g => new
                {
                    Project = g.Key,
                    Amount = g.Sum(t => t.Amount),
                    Count = g.Count(),
                    Percentage = totalIncome > 0 ? Math.Round(g.Sum(t => t.Amount) / totalIncome * 100, 2) : 0
                })
                .OrderByDescending(p => p.Amount)
                .ToList();

            // 按月份分组
            var monthlyStats = transactions
                .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Amount = g.Sum(t => t.Amount),
                    Count = g.Count()
                })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList();

            return new
            {
                TotalIncome = totalIncome,
                TransactionCount = transactions.Count,
                Period = GetPeriodDescription(year, month),
                CategoryStatistics = categoryStats,
                ProjectStatistics = projectStats,
                MonthlyStatistics = monthlyStats,
                TopCategory = categoryStats.FirstOrDefault(),
                TopProject = projectStats.FirstOrDefault()
            };
        }

        // GET: api/Transactions/expense-summary
        [HttpGet("expense-summary")]
        public async Task<ActionResult<object>> GetExpenseSummary([FromQuery] int? year, [FromQuery] int? month)
        {
            var query = _context.Transactions.Where(t => t.TransactionType == "Expense");

            // 应用时间筛选
            if (year.HasValue)
            {
                query = query.Where(t => t.TransactionDate.Year == year.Value);
                
                if (month.HasValue)
                {
                    query = query.Where(t => t.TransactionDate.Month == month.Value);
                }
            }

            var transactions = await query
                .Include(t => t.Project)
                .Include(t => t.ExpenseCategory)
                .ToListAsync();

            // 计算总支出
            decimal totalExpense = transactions.Sum(t => t.Amount);

            // 按支出类别分组
            var categoryStats = transactions
                .GroupBy(t => t.ExpenseCategory?.Name ?? "未分类")
                .Select(g => new
                {
                    Category = g.Key,
                    Amount = g.Sum(t => t.Amount),
                    Count = g.Count(),
                    Percentage = totalExpense > 0 ? Math.Round(g.Sum(t => t.Amount) / totalExpense * 100, 2) : 0
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            // 按项目分组
            var projectStats = transactions
                .GroupBy(t => t.Project?.Name ?? "未分配项目")
                .Select(g => new
                {
                    Project = g.Key,
                    Amount = g.Sum(t => t.Amount),
                    Count = g.Count(),
                    Percentage = totalExpense > 0 ? Math.Round(g.Sum(t => t.Amount) / totalExpense * 100, 2) : 0
                })
                .OrderByDescending(p => p.Amount)
                .ToList();

            // 按月份分组
            var monthlyStats = transactions
                .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Amount = g.Sum(t => t.Amount),
                    Count = g.Count()
                })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList();

            return new
            {
                TotalExpense = totalExpense,
                TransactionCount = transactions.Count,
                Period = GetPeriodDescription(year, month),
                CategoryStatistics = categoryStats,
                ProjectStatistics = projectStats,
                MonthlyStatistics = monthlyStats,
                TopCategory = categoryStats.FirstOrDefault(),
                TopProject = projectStats.FirstOrDefault()
            };
        }

        // GET: api/Transactions/transaction-report
        [HttpGet("transaction-report")]
        public async Task<ActionResult<object>> GetTransactionReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var query = _context.Transactions.AsQueryable();

            // 设置默认时间范围为最近30天
            DateTime reportStartDate = startDate ?? DateTime.Now.AddMonths(-1);
            DateTime reportEndDate = endDate ?? DateTime.Now;

            // 应用时间筛选
            query = query.Where(t => t.TransactionDate >= reportStartDate && t.TransactionDate <= reportEndDate);

            var transactions = await query
                .Include(t => t.Project)
                .Include(t => t.IncomeCategory)
                .Include(t => t.ExpenseCategory)
                .Include(t => t.Account)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            // 计算总收入和总支出
            decimal totalIncome = transactions.Where(t => t.TransactionType == "Income").Sum(t => t.Amount);
            decimal totalExpense = transactions.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount);
            decimal balance = totalIncome - totalExpense;

            // 按日期分组，计算每日收支情况
            var dailyStats = transactions
                .GroupBy(t => t.TransactionDate.Date)
                .Select(g => new
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    Income = g.Where(t => t.TransactionType == "Income").Sum(t => t.Amount),
                    Expense = g.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount),
                    Balance = g.Where(t => t.TransactionType == "Income").Sum(t => t.Amount) - 
                              g.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(d => d.Date)
                .ToList();

            // 按账户分组，计算每个账户的收支情况
            var accountStats = transactions
                .GroupBy(t => t.Account?.Name ?? "未知账户")
                .Select(g => new
                {
                    Account = g.Key,
                    Income = g.Where(t => t.TransactionType == "Income").Sum(t => t.Amount),
                    Expense = g.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount),
                    Balance = g.Where(t => t.TransactionType == "Income").Sum(t => t.Amount) - 
                              g.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(a => a.Balance)
                .ToList();

            // 按项目分组，计算每个项目的收支情况
            var projectStats = transactions
                .GroupBy(t => t.Project?.Name ?? "未分配项目")
                .Select(g => new
                {
                    Project = g.Key,
                    Income = g.Where(t => t.TransactionType == "Income").Sum(t => t.Amount),
                    Expense = g.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount),
                    Balance = g.Where(t => t.TransactionType == "Income").Sum(t => t.Amount) - 
                              g.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(p => p.TransactionCount)
                .ToList();

            return new
            {
                ReportPeriod = $"{reportStartDate.ToString("yyyy-MM-dd")} 至 {reportEndDate.ToString("yyyy-MM-dd")}",
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                Balance = balance,
                TransactionCount = transactions.Count,
                DailyStatistics = dailyStats,
                AccountStatistics = accountStats,
                ProjectStatistics = projectStats,
                TodayTransactions = dailyStats.FirstOrDefault(d => d.Date == DateTime.Now.ToString("yyyy-MM-dd"))
            };
        }

        private string GetPeriodDescription(int? year, int? month)
        {
            if (!year.HasValue)
            {
                return "全部时间";
            }
            
            if (!month.HasValue)
            {
                return $"{year}年";
            }
            
            return $"{year}年{month}月";
        }

        #region 批量操作

        // POST: api/Transactions/Batch
        [HttpPost("Batch")]
        public async Task<IActionResult> CreateTransactionsBatch([FromBody] List<Transaction> transactions)
        {
            if (transactions == null || !transactions.Any())
            {
                return BadRequest("没有提供任何交易记录");
            }

            try
            {
                foreach (var transaction in transactions)
                {
                    // 设置创建时间
                    transaction.CreatedAt = DateTime.Now;

                    // 检查账户是否存在
                    var account = await _context.Accounts.FindAsync(transaction.AccountId);
                    if (account == null)
                    {
                        return BadRequest($"账户ID {transaction.AccountId} 不存在");
                    }

                    // 更新账户余额
                    if (transaction.TransactionType == "收入")
                    {
                        account.Balance += transaction.Amount;
                    }
                    else if (transaction.TransactionType == "支出")
                    {
                        account.Balance -= transaction.Amount;
                    }
                    else
                    {
                        return BadRequest($"无效的交易类型: {transaction.TransactionType}");
                    }

                    _context.Transactions.Add(transaction);
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = $"成功创建 {transactions.Count} 条交易记录" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"批量创建交易记录时发生错误: {ex.Message}");
            }
        }

        // DELETE: api/Transactions/Batch
        [HttpDelete("Batch")]
        public async Task<IActionResult> DeleteTransactionsBatch([FromBody] List<int> transactionIds)
        {
            if (transactionIds == null || !transactionIds.Any())
            {
                return BadRequest("没有提供任何交易记录ID");
            }

            try
            {
                var transactionsToDelete = await _context.Transactions
                    .Where(t => transactionIds.Contains(t.Id))
                    .ToListAsync();

                if (!transactionsToDelete.Any())
                {
                    return NotFound("未找到指定的交易记录");
                }

                // 恢复账户余额
                foreach (var transaction in transactionsToDelete)
                {
                    var account = await _context.Accounts.FindAsync(transaction.AccountId);
                    if (account != null)
                    {
                        if (transaction.TransactionType == "收入")
                        {
                            account.Balance -= transaction.Amount;
                        }
                        else if (transaction.TransactionType == "支出")
                        {
                            account.Balance += transaction.Amount;
                        }
                    }

                    // 检查并删除关联的附件文件
                    if (!string.IsNullOrEmpty(transaction.AttachmentPath))
                    {
                        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), transaction.AttachmentPath);
                        if (System.IO.File.Exists(fullPath))
                        {
                            System.IO.File.Delete(fullPath);
                        }
                    }

                    _context.Transactions.Remove(transaction);
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = $"成功删除 {transactionsToDelete.Count} 条交易记录" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"批量删除交易记录时发生错误: {ex.Message}");
            }
        }

        // PUT: api/Transactions/BatchUpdate
        [HttpPut("BatchUpdate")]
        public async Task<IActionResult> UpdateTransactionsBatch([FromBody] List<Transaction> transactions)
        {
            if (transactions == null || !transactions.Any())
            {
                return BadRequest("没有提供任何交易记录");
            }

            try
            {
                foreach (var transaction in transactions)
                {
                    if (transaction.Id <= 0)
                    {
                        return BadRequest($"交易记录ID无效: {transaction.Id}");
                    }

                    var existingTransaction = await _context.Transactions.FindAsync(transaction.Id);
                    if (existingTransaction == null)
                    {
                        return NotFound($"未找到ID为 {transaction.Id} 的交易记录");
                    }

                    // 保存原始交易信息以便更新账户余额
                    var originalAccount = await _context.Accounts.FindAsync(existingTransaction.AccountId);
                    var originalType = existingTransaction.TransactionType;
                    var originalAmount = existingTransaction.Amount;

                    // 恢复原始账户余额
                    if (originalAccount != null)
                    {
                        if (originalType == "收入")
                        {
                            originalAccount.Balance -= originalAmount;
                        }
                        else if (originalType == "支出")
                        {
                            originalAccount.Balance += originalAmount;
                        }
                    }

                    // 检查新账户是否存在
                    var newAccount = await _context.Accounts.FindAsync(transaction.AccountId);
                    if (newAccount == null)
                    {
                        return BadRequest($"账户ID {transaction.AccountId} 不存在");
                    }

                    // 更新新账户余额
                    if (transaction.TransactionType == "收入")
                    {
                        newAccount.Balance += transaction.Amount;
                    }
                    else if (transaction.TransactionType == "支出")
                    {
                        newAccount.Balance -= transaction.Amount;
                    }
                    else
                    {
                        return BadRequest($"无效的交易类型: {transaction.TransactionType}");
                    }

                    // 更新交易记录属性
                    existingTransaction.ProjectId = transaction.ProjectId;
                    existingTransaction.AccountId = transaction.AccountId;
                    existingTransaction.Amount = transaction.Amount;
                    existingTransaction.TransactionType = transaction.TransactionType;
                    existingTransaction.IncomeCategoryId = transaction.IncomeCategoryId;
                    existingTransaction.ExpenseCategoryId = transaction.ExpenseCategoryId;
                    existingTransaction.Description = transaction.Description;
                    existingTransaction.TransactionDate = transaction.TransactionDate;
                    existingTransaction.TaxRateId = transaction.TaxRateId;
                    // 不更新 CreatedById, CreatedAt 和 AttachmentPath
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = $"成功更新 {transactions.Count} 条交易记录" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"批量更新交易记录时发生错误: {ex.Message}");
            }
        }

        // POST: api/Transactions/BatchTransfer
        [HttpPost("BatchTransfer")]
        public async Task<IActionResult> BatchTransferFunds([FromBody] List<TransferFundsModel> transfers)
        {
            if (transfers == null || !transfers.Any())
            {
                return BadRequest("没有提供任何转账信息");
            }

            try
            {
                foreach (var transfer in transfers)
                {
                    // 验证金额
                    if (transfer.Amount <= 0)
                    {
                        return BadRequest("转账金额必须大于零");
                    }

                    // 验证账户
                    var sourceAccount = await _context.Accounts.FindAsync(transfer.SourceAccountId);
                    if (sourceAccount == null)
                    {
                        return NotFound($"源账户ID {transfer.SourceAccountId} 不存在");
                    }

                    var destinationAccount = await _context.Accounts.FindAsync(transfer.DestinationAccountId);
                    if (destinationAccount == null)
                    {
                        return NotFound($"目标账户ID {transfer.DestinationAccountId} 不存在");
                    }

                    if (sourceAccount.Id == destinationAccount.Id)
                    {
                        return BadRequest("源账户和目标账户不能相同");
                    }

                    // 检查余额
                    if (sourceAccount.Balance < transfer.Amount)
                    {
                        return BadRequest($"源账户 {sourceAccount.Name} 余额不足");
                    }

                    // 创建支出交易
                    var outTransaction = new Transaction
                    {
                        ProjectId = transfer.ProjectId,
                        AccountId = transfer.SourceAccountId,
                        Amount = transfer.Amount,
                        TransactionType = "支出",
                        Description = $"转账至 {destinationAccount.Name}: {transfer.Description}",
                        TransactionDate = transfer.TransactionDate,
                        CreatedById = transfer.CreatedById,
                        CreatedAt = DateTime.Now
                    };

                    // 创建收入交易
                    var inTransaction = new Transaction
                    {
                        ProjectId = transfer.ProjectId,
                        AccountId = transfer.DestinationAccountId,
                        Amount = transfer.Amount,
                        TransactionType = "收入",
                        Description = $"来自 {sourceAccount.Name} 的转账: {transfer.Description}",
                        TransactionDate = transfer.TransactionDate,
                        CreatedById = transfer.CreatedById,
                        CreatedAt = DateTime.Now
                    };

                    // 更新账户余额
                    sourceAccount.Balance -= transfer.Amount;
                    destinationAccount.Balance += transfer.Amount;

                    // 保存到数据库
                    _context.Transactions.Add(outTransaction);
                    _context.Transactions.Add(inTransaction);
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = $"成功批量转账 {transfers.Count} 笔" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"批量转账时发生错误: {ex.Message}");
            }
        }

        #endregion
    }

    // 批量转账模型
    public class TransferFundsModel
    {
        public int SourceAccountId { get; set; }
        public int DestinationAccountId { get; set; }
        public decimal Amount { get; set; }
        public int ProjectId { get; set; }
        public string Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public int CreatedById { get; set; }
    }
}