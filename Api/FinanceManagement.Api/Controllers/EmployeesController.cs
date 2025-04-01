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
    public class EmployeesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EmployeesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Employees
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
        {
            return await _context.Employees.ToListAsync();
        }

        // GET: api/Employees/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            return employee;
        }

        // GET: api/Employees/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetEmployeeStatistics()
        {
            var employees = await _context.Employees.ToListAsync();
            var totalEmployees = employees.Count;
            var activeEmployees = employees.Count(e => e.Status == "Active");
            
            // 获取关联数据
            var salaries = await _context.Salaries
                .Include(s => s.Employee)
                .ToListAsync();
                
            var attendances = await _context.Attendances
                .Include(a => a.Employee)
                .ToListAsync();
                
            var loans = await _context.Loans
                .Include(l => l.Employee)
                .ToListAsync();
            
            // 部门统计
            var departments = employees
                .GroupBy(e => e.Department)
                .Select(g => new
                {
                    Department = g.Key,
                    Count = g.Count(),
                    ActiveCount = g.Count(e => e.Status == "Active"),
                    InactiveCount = g.Count(e => e.Status != "Active"),
                    AverageSalary = g.Average(e => e.BaseSalary),
                    TotalBaseSalary = g.Sum(e => e.BaseSalary),
                    HighestSalary = g.Max(e => e.BaseSalary),
                    LowestSalary = g.Min(e => e.BaseSalary),
                    EmployeeIds = g.Select(e => e.Id).ToList()
                })
                .OrderByDescending(d => d.Count)
                .ToList();
                
            // 职位统计
            var positions = employees
                .GroupBy(e => e.Position)
                .Select(g => new
                {
                    Position = g.Key,
                    Count = g.Count(),
                    AverageSalary = g.Average(e => e.BaseSalary),
                    TotalBaseSalary = g.Sum(e => e.BaseSalary)
                })
                .OrderByDescending(p => p.Count)
                .ToList();
                
            // 计算工资发放统计
            var salarySummary = salaries
                .GroupBy(s => new { Year = s.SalaryMonth.Year, Month = s.SalaryMonth.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Period = $"{g.Key.Year}年{g.Key.Month}月",
                    EmployeeCount = g.Count(),
                    TotalBasicSalary = g.Sum(s => s.BasicSalary),
                    TotalBonus = g.Sum(s => s.Bonus),
                    TotalDeduction = g.Sum(s => s.Deduction),
                    TotalActualPayment = g.Sum(s => s.ActualPayment)
                })
                .OrderByDescending(s => s.Year)
                .ThenByDescending(s => s.Month)
                .Take(12) // 最近12个月
                .ToList();
                
            // 考勤统计
            var attendanceSummary = attendances
                .GroupBy(a => a.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / attendances.Count * 100
                })
                .OrderByDescending(a => a.Count)
                .ToList();
                
            // 员工借款统计
            var loanSummary = new
            {
                TotalLoanAmount = loans.Sum(l => l.LoanAmount),
                AverageLoanAmount = loans.Count > 0 ? loans.Average(l => l.LoanAmount) : 0,
                ActiveLoans = loans.Count(l => l.Status == "Active"),
                CompletedLoans = loans.Count(l => l.Status == "Completed"),
                EmployeesWithLoans = loans.Select(l => l.EmployeeId).Distinct().Count()
            };
                
            // 入职年限分布
            var tenureBands = new[]
            {
                new { Band = "1年以下", Count = employees.Count(e => (DateTime.Now - e.HireDate).TotalDays < 365) },
                new { Band = "1-3年", Count = employees.Count(e => (DateTime.Now - e.HireDate).TotalDays >= 365 && (DateTime.Now - e.HireDate).TotalDays < 365 * 3) },
                new { Band = "3-5年", Count = employees.Count(e => (DateTime.Now - e.HireDate).TotalDays >= 365 * 3 && (DateTime.Now - e.HireDate).TotalDays < 365 * 5) },
                new { Band = "5-10年", Count = employees.Count(e => (DateTime.Now - e.HireDate).TotalDays >= 365 * 5 && (DateTime.Now - e.HireDate).TotalDays < 365 * 10) },
                new { Band = "10年以上", Count = employees.Count(e => (DateTime.Now - e.HireDate).TotalDays >= 365 * 10) }
            };

            return new
            {
                TotalEmployees = totalEmployees,
                ActiveEmployees = activeEmployees,
                InactiveEmployees = totalEmployees - activeEmployees,
                DepartmentStatistics = departments,
                PositionStatistics = positions,
                AverageBaseSalary = employees.Count > 0 ? employees.Average(e => e.BaseSalary) : 0,
                TotalBaseSalary = employees.Sum(e => e.BaseSalary),
                HighestPaidEmployee = employees.OrderByDescending(e => e.BaseSalary).FirstOrDefault(),
                SalaryStatistics = salarySummary,
                AttendanceStatistics = attendanceSummary,
                LoanStatistics = loanSummary,
                TenureDistribution = tenureBands,
                NewEmployeesThisMonth = employees.Count(e => e.HireDate.Year == DateTime.Now.Year && e.HireDate.Month == DateTime.Now.Month),
                NewEmployeesThisYear = employees.Count(e => e.HireDate.Year == DateTime.Now.Year)
            };
        }

        // POST: api/Employees
        [HttpPost]
        public async Task<ActionResult<Employee>> CreateEmployee(Employee employee)
        {
            employee.CreatedAt = DateTime.Now;
            
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
        }

        // PUT: api/Employees/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, Employee employee)
        {
            if (id != employee.Id)
            {
                return BadRequest();
            }

            _context.Entry(employee).State = EntityState.Modified;

            // 保持创建时间不变
            _context.Entry(employee).Property(e => e.CreatedAt).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await EmployeeExists(id))
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

        // DELETE: api/Employees/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            // 检查是否有关联的工资记录
            if (await _context.Salaries.AnyAsync(s => s.EmployeeId == id))
            {
                return BadRequest("无法删除已有工资记录的员工");
            }

            // 检查是否有关联的考勤记录
            if (await _context.Attendances.AnyAsync(a => a.EmployeeId == id))
            {
                return BadRequest("无法删除已有考勤记录的员工");
            }

            // 检查是否有关联的借款记录
            if (await _context.Loans.AnyAsync(l => l.EmployeeId == id))
            {
                return BadRequest("无法删除已有借款记录的员工");
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> EmployeeExists(int id)
        {
            return await _context.Employees.AnyAsync(e => e.Id == id);
        }
    }
} 