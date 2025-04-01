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
    public class InvoicesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InvoicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Invoices
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetInvoices()
        {
            return await _context.Invoices
                .Include(i => i.Contract)
                .Include(i => i.InvoiceType)
                .Include(i => i.TaxRate)
                .ToListAsync();
        }

        // GET: api/Invoices/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Invoice>> GetInvoice(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Contract)
                .Include(i => i.InvoiceType)
                .Include(i => i.TaxRate)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            return invoice;
        }

        // GET: api/Invoices/Sales
        [HttpGet("Sales")]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetSalesInvoices()
        {
            return await _context.Invoices
                .Include(i => i.Contract)
                .Include(i => i.InvoiceType)
                .Include(i => i.TaxRate)
                .Where(i => i.InvoiceType_ == "Sales")
                .ToListAsync();
        }

        // GET: api/Invoices/Purchase
        [HttpGet("Purchase")]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetPurchaseInvoices()
        {
            return await _context.Invoices
                .Include(i => i.Contract)
                .Include(i => i.InvoiceType)
                .Include(i => i.TaxRate)
                .Where(i => i.InvoiceType_ == "Purchase")
                .ToListAsync();
        }

        // POST: api/Invoices
        [HttpPost]
        public async Task<ActionResult<Invoice>> CreateInvoice(Invoice invoice)
        {
            // 验证合同是否存在
            var contract = await _context.Contracts.FindAsync(invoice.ContractId);
            if (contract == null)
            {
                return BadRequest("指定的合同不存在");
            }
            
            // 验证发票类型是否存在
            if (!await _context.InvoiceTypes.AnyAsync(it => it.Id == invoice.InvoiceTypeId))
            {
                return BadRequest("指定的发票类型不存在");
            }

            // 验证税率是否存在（如果提供了税率ID）
            if (invoice.TaxRateId.HasValue && !await _context.TaxRates.AnyAsync(t => t.Id == invoice.TaxRateId))
            {
                return BadRequest("指定的税率不存在");
            }

            // 确保发票类型与合同类型一致
            if (invoice.InvoiceType_ != contract.ContractType)
            {
                return BadRequest("发票类型必须与合同类型一致");
            }

            // 计算不含税金额和税额
            if (invoice.TaxRateId.HasValue)
            {
                var taxRate = await _context.TaxRates.FindAsync(invoice.TaxRateId);
                
                if (taxRate != null)
                {
                    // 设置税率相关金额
                    invoice.AmountWithoutTax = Math.Round(invoice.Amount / (1 + taxRate.Rate / 100), 2);
                    invoice.TaxAmount = invoice.Amount - invoice.AmountWithoutTax;
                }
            }
            else
            {
                // 无税率时，税额为0
                invoice.AmountWithoutTax = invoice.Amount;
                invoice.TaxAmount = 0;
            }

            invoice.CreatedAt = DateTime.Now;
            
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // 生成对应的交易记录
            await CreateTransactionFromInvoice(invoice);

            return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
        }

        // PUT: api/Invoices/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInvoice(int id, Invoice invoice)
        {
            if (id != invoice.Id)
            {
                return BadRequest();
            }

            // 验证合同是否存在
            var contract = await _context.Contracts.FindAsync(invoice.ContractId);
            if (contract == null)
            {
                return BadRequest("指定的合同不存在");
            }
            
            // 验证发票类型是否存在
            if (!await _context.InvoiceTypes.AnyAsync(it => it.Id == invoice.InvoiceTypeId))
            {
                return BadRequest("指定的发票类型不存在");
            }

            // 验证税率是否存在（如果提供了税率ID）
            if (invoice.TaxRateId.HasValue && !await _context.TaxRates.AnyAsync(t => t.Id == invoice.TaxRateId))
            {
                return BadRequest("指定的税率不存在");
            }

            // 确保发票类型与合同类型一致
            if (invoice.InvoiceType_ != contract.ContractType)
            {
                return BadRequest("发票类型必须与合同类型一致");
            }

            // 重新计算不含税金额和税额
            if (invoice.TaxRateId.HasValue)
            {
                var taxRate = await _context.TaxRates.FindAsync(invoice.TaxRateId);
                
                if (taxRate != null)
                {
                    // 设置税率相关金额
                    invoice.AmountWithoutTax = Math.Round(invoice.Amount / (1 + taxRate.Rate / 100), 2);
                    invoice.TaxAmount = invoice.Amount - invoice.AmountWithoutTax;
                }
            }
            else
            {
                // 无税率时，税额为0
                invoice.AmountWithoutTax = invoice.Amount;
                invoice.TaxAmount = 0;
            }

            _context.Entry(invoice).State = EntityState.Modified;

            // 保持创建时间不变
            _context.Entry(invoice).Property(i => i.CreatedAt).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await InvoiceExists(id))
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

        // DELETE: api/Invoices/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteInvoice(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            // 查找关联的交易记录并删除
            var relatedTransactions = await _context.Transactions
                .Where(t => t.Description.Contains($"发票编号: {invoice.InvoiceNumber}"))
                .ToListAsync();

            if (relatedTransactions.Any())
            {
                foreach (var transaction in relatedTransactions)
                {
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
                }
            }

            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task CreateTransactionFromInvoice(Invoice invoice)
        {
            // 获取合同关联的项目
            var contract = await _context.Contracts
                .Include(c => c.Project)
                .FirstOrDefaultAsync(c => c.Id == invoice.ContractId);

            if (contract?.Project == null)
            {
                return; // 无法创建交易记录
            }

            // 确定默认账户（这里简化为取第一个账户）
            var defaultAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.IsActive);
            if (defaultAccount == null)
            {
                return; // 无法创建交易记录
            }

            // 创建交易记录
            var transaction = new Transaction
            {
                ProjectId = contract.Project.Id,
                AccountId = defaultAccount.Id,
                Amount = invoice.Amount,
                TransactionType = invoice.InvoiceType_ == "Sales" ? "Income" : "Expense",
                Description = $"发票交易 - 发票编号: {invoice.InvoiceNumber}",
                TransactionDate = invoice.InvoiceDate,
                CreatedById = int.Parse(User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value),
                CreatedAt = DateTime.Now,
                TaxRateId = invoice.TaxRateId
            };

            // 设置收入或支出科目
            if (transaction.TransactionType == "Income")
            {
                // 获取第一个收入科目作为默认（此处简化处理）
                var defaultIncomeCategory = await _context.IncomeCategories.FirstOrDefaultAsync(ic => ic.IsActive);
                if (defaultIncomeCategory != null)
                {
                    transaction.IncomeCategoryId = defaultIncomeCategory.Id;
                }
            }
            else
            {
                // 获取第一个支出科目作为默认（此处简化处理）
                var defaultExpenseCategory = await _context.ExpenseCategories.FirstOrDefaultAsync(ec => ec.IsActive);
                if (defaultExpenseCategory != null)
                {
                    transaction.ExpenseCategoryId = defaultExpenseCategory.Id;
                }
            }

            // 添加交易记录
            _context.Transactions.Add(transaction);

            // 更新账户余额
            if (transaction.TransactionType == "Income")
            {
                defaultAccount.Balance += transaction.Amount;
            }
            else
            {
                defaultAccount.Balance -= transaction.Amount;
            }

            await _context.SaveChangesAsync();
        }

        private async Task<bool> InvoiceExists(int id)
        {
            return await _context.Invoices.AnyAsync(e => e.Id == id);
        }
    }
} 