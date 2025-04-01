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
    public class LoansController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LoansController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Loans
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Loan>>> GetLoans()
        {
            return await _context.Loans
                .Include(l => l.Employee)
                .OrderByDescending(l => l.LoanDate)
                .ToListAsync();
        }

        // GET: api/Loans/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Loan>> GetLoan(int id)
        {
            var loan = await _context.Loans
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null)
            {
                return NotFound();
            }

            return loan;
        }

        // GET: api/Loans/Employee/5
        [HttpGet("Employee/{employeeId}")]
        public async Task<ActionResult<IEnumerable<Loan>>> GetEmployeeLoans(int employeeId)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
            {
                return NotFound("指定的员工不存在");
            }

            return await _context.Loans
                .Where(l => l.EmployeeId == employeeId)
                .OrderByDescending(l => l.LoanDate)
                .ToListAsync();
        }

        // POST: api/Loans
        [HttpPost]
        public async Task<ActionResult<Loan>> CreateLoan(Loan loan)
        {
            // 验证员工是否存在
            var employee = await _context.Employees.FindAsync(loan.EmployeeId);
            if (employee == null)
            {
                return BadRequest("指定的员工不存在");
            }

            // 设置状态为待审批
            if (string.IsNullOrEmpty(loan.Status))
            {
                loan.Status = "Pending";
            }
            
            // 设置创建时间
            loan.CreatedAt = DateTime.Now;
            
            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLoan), new { id = loan.Id }, loan);
        }

        // PUT: api/Loans/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLoan(int id, Loan loan)
        {
            if (id != loan.Id)
            {
                return BadRequest();
            }

            _context.Entry(loan).State = EntityState.Modified;

            // 保持创建时间不变
            _context.Entry(loan).Property(l => l.CreatedAt).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await LoanExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // PUT: api/Loans/5/Approve
        [HttpPut("{id}/Approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveLoan(int id)
        {
            var loan = await _context.Loans.FindAsync(id);
            if (loan == null)
            {
                return NotFound();
            }

            if (loan.Status != "Pending")
            {
                return BadRequest("只能审批状态为'待审批'的借款");
            }

            loan.Status = "Approved";
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/Loans/5/Complete
        [HttpPut("{id}/Complete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CompleteLoan(int id)
        {
            var loan = await _context.Loans.FindAsync(id);
            if (loan == null)
            {
                return NotFound();
            }

            loan.Status = "Completed";
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Loans/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteLoan(int id)
        {
            var loan = await _context.Loans.FindAsync(id);
            if (loan == null)
            {
                return NotFound();
            }

            // 只能删除待审批或被拒绝的借款
            if (loan.Status != "Pending" && loan.Status != "Rejected")
            {
                return BadRequest("只能删除状态为'待审批'或'已拒绝'的借款");
            }

            _context.Loans.Remove(loan);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> LoanExists(int id)
        {
            return await _context.Loans.AnyAsync(e => e.Id == id);
        }
    }
} 