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
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region 收入科目管理

        // GET: api/Categories/Income
        [HttpGet("Income")]
        public async Task<ActionResult<IEnumerable<IncomeCategory>>> GetIncomeCategories()
        {
            return await _context.IncomeCategories.ToListAsync();
        }

        // GET: api/Categories/Income/5
        [HttpGet("Income/{id}")]
        public async Task<ActionResult<IncomeCategory>> GetIncomeCategory(int id)
        {
            var incomeCategory = await _context.IncomeCategories.FindAsync(id);

            if (incomeCategory == null)
            {
                return NotFound();
            }

            return incomeCategory;
        }

        // POST: api/Categories/Income
        [HttpPost("Income")]
        public async Task<ActionResult<IncomeCategory>> CreateIncomeCategory(IncomeCategory incomeCategory)
        {
            incomeCategory.CreatedAt = DateTime.Now;
            
            _context.IncomeCategories.Add(incomeCategory);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetIncomeCategory), new { id = incomeCategory.Id }, incomeCategory);
        }

        // PUT: api/Categories/Income/5
        [HttpPut("Income/{id}")]
        public async Task<IActionResult> UpdateIncomeCategory(int id, IncomeCategory incomeCategory)
        {
            if (id != incomeCategory.Id)
            {
                return BadRequest();
            }

            _context.Entry(incomeCategory).State = EntityState.Modified;

            // 保持创建时间不变
            _context.Entry(incomeCategory).Property(c => c.CreatedAt).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await IncomeCategoryExists(id))
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

        // DELETE: api/Categories/Income/5
        [HttpDelete("Income/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteIncomeCategory(int id)
        {
            var incomeCategory = await _context.IncomeCategories.FindAsync(id);
            if (incomeCategory == null)
            {
                return NotFound();
            }

            // 检查是否有关联的交易记录
            if (await _context.Transactions.AnyAsync(t => t.IncomeCategoryId == id))
            {
                return BadRequest("无法删除已有交易记录的收入科目");
            }

            _context.IncomeCategories.Remove(incomeCategory);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> IncomeCategoryExists(int id)
        {
            return await _context.IncomeCategories.AnyAsync(e => e.Id == id);
        }

        #endregion

        #region 支出科目管理

        // GET: api/Categories/Expense
        [HttpGet("Expense")]
        public async Task<ActionResult<IEnumerable<ExpenseCategory>>> GetExpenseCategories()
        {
            return await _context.ExpenseCategories.ToListAsync();
        }

        // GET: api/Categories/Expense/5
        [HttpGet("Expense/{id}")]
        public async Task<ActionResult<ExpenseCategory>> GetExpenseCategory(int id)
        {
            var expenseCategory = await _context.ExpenseCategories.FindAsync(id);

            if (expenseCategory == null)
            {
                return NotFound();
            }

            return expenseCategory;
        }

        // POST: api/Categories/Expense
        [HttpPost("Expense")]
        public async Task<ActionResult<ExpenseCategory>> CreateExpenseCategory(ExpenseCategory expenseCategory)
        {
            expenseCategory.CreatedAt = DateTime.Now;
            
            _context.ExpenseCategories.Add(expenseCategory);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetExpenseCategory), new { id = expenseCategory.Id }, expenseCategory);
        }

        // PUT: api/Categories/Expense/5
        [HttpPut("Expense/{id}")]
        public async Task<IActionResult> UpdateExpenseCategory(int id, ExpenseCategory expenseCategory)
        {
            if (id != expenseCategory.Id)
            {
                return BadRequest();
            }

            _context.Entry(expenseCategory).State = EntityState.Modified;

            // 保持创建时间不变
            _context.Entry(expenseCategory).Property(c => c.CreatedAt).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ExpenseCategoryExists(id))
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

        // DELETE: api/Categories/Expense/5
        [HttpDelete("Expense/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteExpenseCategory(int id)
        {
            var expenseCategory = await _context.ExpenseCategories.FindAsync(id);
            if (expenseCategory == null)
            {
                return NotFound();
            }

            // 检查是否有关联的交易记录
            if (await _context.Transactions.AnyAsync(t => t.ExpenseCategoryId == id))
            {
                return BadRequest("无法删除已有交易记录的支出科目");
            }

            _context.ExpenseCategories.Remove(expenseCategory);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> ExpenseCategoryExists(int id)
        {
            return await _context.ExpenseCategories.AnyAsync(e => e.Id == id);
        }

        #endregion

        #region 发票类型管理

        // GET: api/Categories/InvoiceType
        [HttpGet("InvoiceType")]
        public async Task<ActionResult<IEnumerable<InvoiceType>>> GetInvoiceTypes()
        {
            return await _context.InvoiceTypes.ToListAsync();
        }

        // GET: api/Categories/InvoiceType/5
        [HttpGet("InvoiceType/{id}")]
        public async Task<ActionResult<InvoiceType>> GetInvoiceType(int id)
        {
            var invoiceType = await _context.InvoiceTypes.FindAsync(id);

            if (invoiceType == null)
            {
                return NotFound();
            }

            return invoiceType;
        }

        // POST: api/Categories/InvoiceType
        [HttpPost("InvoiceType")]
        public async Task<ActionResult<InvoiceType>> CreateInvoiceType(InvoiceType invoiceType)
        {
            invoiceType.CreatedAt = DateTime.Now;
            
            _context.InvoiceTypes.Add(invoiceType);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetInvoiceType), new { id = invoiceType.Id }, invoiceType);
        }

        // PUT: api/Categories/InvoiceType/5
        [HttpPut("InvoiceType/{id}")]
        public async Task<IActionResult> UpdateInvoiceType(int id, InvoiceType invoiceType)
        {
            if (id != invoiceType.Id)
            {
                return BadRequest();
            }

            _context.Entry(invoiceType).State = EntityState.Modified;

            // 保持创建时间不变
            _context.Entry(invoiceType).Property(t => t.CreatedAt).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await InvoiceTypeExists(id))
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

        // DELETE: api/Categories/InvoiceType/5
        [HttpDelete("InvoiceType/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteInvoiceType(int id)
        {
            var invoiceType = await _context.InvoiceTypes.FindAsync(id);
            if (invoiceType == null)
            {
                return NotFound();
            }

            // 检查是否有关联的发票
            if (await _context.Invoices.AnyAsync(i => i.InvoiceTypeId == id))
            {
                return BadRequest("无法删除已有发票的类型");
            }

            _context.InvoiceTypes.Remove(invoiceType);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> InvoiceTypeExists(int id)
        {
            return await _context.InvoiceTypes.AnyAsync(e => e.Id == id);
        }

        #endregion

        #region 税率管理

        // GET: api/Categories/TaxRate
        [HttpGet("TaxRate")]
        public async Task<ActionResult<IEnumerable<TaxRate>>> GetTaxRates()
        {
            return await _context.TaxRates.ToListAsync();
        }

        // GET: api/Categories/TaxRate/5
        [HttpGet("TaxRate/{id}")]
        public async Task<ActionResult<TaxRate>> GetTaxRate(int id)
        {
            var taxRate = await _context.TaxRates.FindAsync(id);

            if (taxRate == null)
            {
                return NotFound();
            }

            return taxRate;
        }

        // POST: api/Categories/TaxRate
        [HttpPost("TaxRate")]
        public async Task<ActionResult<TaxRate>> CreateTaxRate(TaxRate taxRate)
        {
            taxRate.CreatedAt = DateTime.Now;
            
            _context.TaxRates.Add(taxRate);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTaxRate), new { id = taxRate.Id }, taxRate);
        }

        // PUT: api/Categories/TaxRate/5
        [HttpPut("TaxRate/{id}")]
        public async Task<IActionResult> UpdateTaxRate(int id, TaxRate taxRate)
        {
            if (id != taxRate.Id)
            {
                return BadRequest();
            }

            _context.Entry(taxRate).State = EntityState.Modified;

            // 保持创建时间不变
            _context.Entry(taxRate).Property(t => t.CreatedAt).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TaxRateExists(id))
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

        // DELETE: api/Categories/TaxRate/5
        [HttpDelete("TaxRate/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTaxRate(int id)
        {
            var taxRate = await _context.TaxRates.FindAsync(id);
            if (taxRate == null)
            {
                return NotFound();
            }

            // 检查是否有关联的交易记录
            if (await _context.Transactions.AnyAsync(t => t.TaxRateId == id))
            {
                return BadRequest("无法删除已有交易记录的税率");
            }

            // 检查是否有关联的发票
            if (await _context.Invoices.AnyAsync(i => i.TaxRateId == id))
            {
                return BadRequest("无法删除已有发票的税率");
            }

            // 检查是否有关联的合同
            if (await _context.Contracts.AnyAsync(c => c.TaxRateId == id))
            {
                return BadRequest("无法删除已有合同的税率");
            }

            _context.TaxRates.Remove(taxRate);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> TaxRateExists(int id)
        {
            return await _context.TaxRates.AnyAsync(e => e.Id == id);
        }

        #endregion
    }
} 