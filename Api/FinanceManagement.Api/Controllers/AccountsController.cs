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
    public class AccountsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AccountsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Accounts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Account>>> GetAccounts()
        {
            return await _context.Accounts.ToListAsync();
        }

        // GET: api/Accounts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Account>> GetAccount(int id)
        {
            var account = await _context.Accounts.FindAsync(id);

            if (account == null)
            {
                return NotFound();
            }

            return account;
        }

        // GET: api/Accounts/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetAccountsStatistics()
        {
            var accounts = await _context.Accounts.ToListAsync();
            var totalAccounts = accounts.Count;
            var totalBalance = accounts.Sum(a => a.Balance);
            var activeAccounts = accounts.Count(a => a.IsActive);
            
            // 查询所有交易记录
            var transactions = await _context.Transactions
                .Include(t => t.Account)
                .ToListAsync();
            
            // 统计每个账户的交易情况
            var accountDetails = accounts.Select(a => new
            {
                AccountId = a.Id,
                AccountName = a.Name,
                AccountNumber = a.AccountNumber,
                AccountType = a.AccountType,
                Balance = a.Balance,
                IsActive = a.IsActive,
                TransactionCount = transactions.Count(t => t.AccountId == a.Id),
                IncomeAmount = transactions.Where(t => t.AccountId == a.Id && t.TransactionType == "Income").Sum(t => t.Amount),
                ExpenseAmount = transactions.Where(t => t.AccountId == a.Id && t.TransactionType == "Expense").Sum(t => t.Amount),
                LastTransaction = transactions.Where(t => t.AccountId == a.Id)
                    .OrderByDescending(t => t.TransactionDate)
                    .Select(t => new { t.TransactionDate, t.Amount, t.TransactionType })
                    .FirstOrDefault()
            }).ToList();
            
            // 按账户类型分组
            var accountsByType = accounts
                .GroupBy(a => a.AccountType)
                .Select(g => new
                {
                    AccountType = g.Key,
                    Count = g.Count(),
                    TotalBalance = g.Sum(a => a.Balance),
                    AverageBalance = g.Count() > 0 ? g.Sum(a => a.Balance) / g.Count() : 0,
                    ActiveAccounts = g.Count(a => a.IsActive),
                    HighestBalance = g.Max(a => a.Balance),
                    LowestBalance = g.Min(a => a.Balance)
                })
                .OrderByDescending(at => at.TotalBalance)
                .ToList();

            // 计算月度交易统计
            var monthlyTransactions = transactions
                .GroupBy(t => new { Year = t.TransactionDate.Year, Month = t.TransactionDate.Month })
                .Select(g => new 
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TransactionCount = g.Count(),
                    Income = g.Where(t => t.TransactionType == "Income").Sum(t => t.Amount),
                    Expense = g.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount),
                    NetChange = g.Where(t => t.TransactionType == "Income").Sum(t => t.Amount) - 
                              g.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount)
                })
                .OrderByDescending(m => m.Year)
                .ThenByDescending(m => m.Month)
                .Take(12) // 最近12个月
                .ToList();

            return new
            {
                TotalAccounts = totalAccounts,
                ActiveAccounts = activeAccounts,
                InactiveAccounts = totalAccounts - activeAccounts,
                TotalBalance = totalBalance,
                AverageBalance = totalAccounts > 0 ? totalBalance / totalAccounts : 0,
                HighestBalanceAccount = accounts.OrderByDescending(a => a.Balance).FirstOrDefault(),
                AccountsByType = accountsByType,
                AccountDetails = accountDetails,
                MonthlyTransactions = monthlyTransactions,
                TotalTransactions = transactions.Count,
                RecentTransactions = transactions
                    .OrderByDescending(t => t.TransactionDate)
                    .Take(5)
                    .Select(t => new 
                    { 
                        t.Id, 
                        t.TransactionDate, 
                        t.Amount, 
                        t.TransactionType, 
                        AccountName = t.Account?.Name ?? "未知账户"
                    })
                    .ToList()
            };
        }

        // POST: api/Accounts
        [HttpPost]
        public async Task<ActionResult<Account>> CreateAccount(Account account)
        {
            account.CreatedAt = DateTime.Now;
            
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, account);
        }

        // PUT: api/Accounts/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAccount(int id, Account account)
        {
            if (id != account.Id)
            {
                return BadRequest();
            }

            // 获取原账户以检查余额变更
            var originalAccount = await _context.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (originalAccount == null)
            {
                return NotFound();
            }

            // 如果余额变更，记录一个系统交易
            if (originalAccount.Balance != account.Balance)
            {
                // 记录余额调整交易
                var transaction = new Transaction
                {
                    ProjectId = 1, // 假设有一个系统项目ID为1
                    AccountId = id,
                    Amount = Math.Abs(account.Balance - originalAccount.Balance),
                    TransactionType = account.Balance > originalAccount.Balance ? "Income" : "Expense",
                    Description = "账户余额系统调整",
                    TransactionDate = DateTime.Now,
                    CreatedById = int.Parse(User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value),
                    CreatedAt = DateTime.Now
                };

                _context.Transactions.Add(transaction);
            }

            _context.Entry(account).State = EntityState.Modified;

            // 保持创建时间不变
            _context.Entry(account).Property(a => a.CreatedAt).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await AccountExists(id))
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

        // DELETE: api/Accounts/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
            {
                return NotFound();
            }

            // 检查账户是否有关联的交易记录
            if (await _context.Transactions.AnyAsync(t => t.AccountId == id))
            {
                return BadRequest("无法删除已有交易记录的账户");
            }

            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> AccountExists(int id)
        {
            return await _context.Accounts.AnyAsync(e => e.Id == id);
        }
    }
} 