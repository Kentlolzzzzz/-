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
    public class SalariesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SalariesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Salaries
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Salary>>> GetSalaries()
        {
            return await _context.Salaries
                .Include(s => s.Employee)
                .ToListAsync();
        }

        // GET: api/Salaries/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Salary>> GetSalary(int id)
        {
            var salary = await _context.Salaries
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (salary == null)
            {
                return NotFound();
            }

            return salary;
        }

        // GET: api/Salaries/Employee/5
        [HttpGet("Employee/{employeeId}")]
        public async Task<ActionResult<IEnumerable<Salary>>> GetEmployeeSalaries(int employeeId)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
            {
                return NotFound("指定的员工不存在");
            }

            return await _context.Salaries
                .Where(s => s.EmployeeId == employeeId)
                .OrderByDescending(s => s.SalaryMonth)
                .ToListAsync();
        }

        // GET: api/Salaries/Month/2024-03
        [HttpGet("Month/{yearMonth}")]
        public async Task<ActionResult<IEnumerable<Salary>>> GetMonthlySalaries(string yearMonth)
        {
            if (!DateTime.TryParse($"{yearMonth}-01", out DateTime date))
            {
                return BadRequest("日期格式不正确，应为 yyyy-MM");
            }

            return await _context.Salaries
                .Include(s => s.Employee)
                .Where(s => s.SalaryMonth.Year == date.Year && s.SalaryMonth.Month == date.Month)
                .OrderBy(s => s.Employee.Name)
                .ToListAsync();
        }

        // POST: api/Salaries
        [HttpPost]
        public async Task<ActionResult<Salary>> CreateSalary(Salary salary)
        {
            // 验证员工是否存在
            var employee = await _context.Employees.FindAsync(salary.EmployeeId);
            if (employee == null)
            {
                return BadRequest("指定的员工不存在");
            }

            // 验证同一个月份不能重复添加同一员工的工资记录
            bool duplicateSalary = await _context.Salaries.AnyAsync(s => 
                s.EmployeeId == salary.EmployeeId && 
                s.SalaryMonth.Year == salary.SalaryMonth.Year && 
                s.SalaryMonth.Month == salary.SalaryMonth.Month);
                
            if (duplicateSalary)
            {
                return BadRequest("该员工在此月份已有工资记录");
            }

            // 设置创建时间
            salary.CreatedAt = DateTime.Now;
            
            _context.Salaries.Add(salary);
            await _context.SaveChangesAsync();

            // 如果状态是已支付，创建一条支出交易记录
            if (salary.PaymentStatus == "Paid")
            {
                await CreateSalaryTransaction(salary);
            }

            return CreatedAtAction(nameof(GetSalary), new { id = salary.Id }, salary);
        }

        // PUT: api/Salaries/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSalary(int id, Salary salary)
        {
            if (id != salary.Id)
            {
                return BadRequest();
            }

            // 获取原工资记录
            var originalSalary = await _context.Salaries.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            if (originalSalary == null)
            {
                return NotFound();
            }

            _context.Entry(salary).State = EntityState.Modified;

            // 保持创建时间不变
            _context.Entry(salary).Property(s => s.CreatedAt).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();

                // 如果支付状态从未支付变为已支付，创建一条支出交易记录
                if (originalSalary.PaymentStatus != "Paid" && salary.PaymentStatus == "Paid")
                {
                    await CreateSalaryTransaction(salary);
                }

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await SalaryExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // POST: api/Salaries/GenerateMonthly/2024-03
        [HttpPost("GenerateMonthly/{yearMonth}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Salary>>> GenerateMonthlySalaries(string yearMonth)
        {
            if (!DateTime.TryParse($"{yearMonth}-01", out DateTime salaryMonth))
            {
                return BadRequest("日期格式不正确，应为 yyyy-MM");
            }

            // 获取所有在职员工
            var activeEmployees = await _context.Employees
                .Where(e => e.Status == "Active")
                .ToListAsync();

            if (!activeEmployees.Any())
            {
                return NotFound("没有在职员工");
            }

            // 检查是否已经生成了本月的工资记录
            var existingSalaries = await _context.Salaries
                .Where(s => s.SalaryMonth.Year == salaryMonth.Year && s.SalaryMonth.Month == salaryMonth.Month)
                .Select(s => s.EmployeeId)
                .ToListAsync();

            // 记录创建的新工资记录
            var createdSalaries = new List<Salary>();

            // 为每个没有生成工资记录的员工创建记录
            foreach (var employee in activeEmployees)
            {
                if (existingSalaries.Contains(employee.Id))
                {
                    continue; // 跳过已有工资记录的员工
                }

                // 获取员工当月考勤情况
                var attendances = await _context.Attendances
                    .Where(a => a.EmployeeId == employee.Id && 
                           a.AttendanceDate.Year == salaryMonth.Year && 
                           a.AttendanceDate.Month == salaryMonth.Month)
                    .ToListAsync();

                // 计算工资（这里简化计算，实际可能更复杂）
                decimal bonus = 0;
                decimal deduction = 0;

                // 缺勤扣款
                int workDays = DateTime.DaysInMonth(salaryMonth.Year, salaryMonth.Month);
                int attendanceDays = attendances.Count(a => a.Status == "Normal");
                decimal dailySalary = employee.BaseSalary / workDays;
                deduction = (workDays - attendanceDays) * dailySalary;

                // 创建工资记录
                var salary = new Salary
                {
                    EmployeeId = employee.Id,
                    SalaryMonth = salaryMonth,
                    BasicSalary = employee.BaseSalary,
                    Bonus = bonus,
                    Deduction = deduction,
                    ActualPayment = employee.BaseSalary + bonus - deduction,
                    PaymentStatus = "Pending",
                    CreatedAt = DateTime.Now
                };

                _context.Salaries.Add(salary);
                createdSalaries.Add(salary);
            }

            if (createdSalaries.Any())
            {
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetMonthlySalaries), new { yearMonth }, createdSalaries);
            }
            else
            {
                return BadRequest("所有员工已存在当月工资记录");
            }
        }

        // POST: api/Salaries/PaySalaries/2024-03
        [HttpPost("PaySalaries/{yearMonth}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PayMonthlySalaries(string yearMonth)
        {
            if (!DateTime.TryParse($"{yearMonth}-01", out DateTime salaryMonth))
            {
                return BadRequest("日期格式不正确，应为 yyyy-MM");
            }

            // 获取所有待支付的工资记录
            var pendingSalaries = await _context.Salaries
                .Where(s => s.SalaryMonth.Year == salaryMonth.Year && 
                            s.SalaryMonth.Month == salaryMonth.Month && 
                            s.PaymentStatus == "Pending")
                .ToListAsync();

            if (!pendingSalaries.Any())
            {
                return NotFound("没有待支付的工资记录");
            }

            // 更新所有待支付的工资记录为已支付
            foreach (var salary in pendingSalaries)
            {
                salary.PaymentStatus = "Paid";
                salary.PaymentDate = DateTime.Now;

                // 创建交易记录
                await CreateSalaryTransaction(salary);
            }

            await _context.SaveChangesAsync();

            return Ok($"成功支付 {pendingSalaries.Count} 条工资记录");
        }

        private async Task CreateSalaryTransaction(Salary salary)
        {
            // 获取员工信息
            var employee = await _context.Employees.FindAsync(salary.EmployeeId);
            if (employee == null) return;

            // 确定默认账户（这里简化为取第一个账户）
            var defaultAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.IsActive);
            if (defaultAccount == null) return;

            // 获取第一个支出科目作为默认（此处简化处理）
            var defaultExpenseCategory = await _context.ExpenseCategories.FirstOrDefaultAsync(ec => ec.IsActive);
            if (defaultExpenseCategory == null) return;

            // 创建交易记录
            var transaction = new Transaction
            {
                ProjectId = 1, // 假设有一个系统项目ID为1，实际应该有一个专门的工资项目
                AccountId = defaultAccount.Id,
                Amount = salary.ActualPayment,
                TransactionType = "Expense",
                ExpenseCategoryId = defaultExpenseCategory.Id,
                Description = $"员工工资 - {employee.Name} - {salary.SalaryMonth.ToString("yyyy-MM")}",
                TransactionDate = DateTime.Now,
                CreatedById = int.Parse(User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value),
                CreatedAt = DateTime.Now
            };

            _context.Transactions.Add(transaction);

            // 更新账户余额
            defaultAccount.Balance -= transaction.Amount;

            await _context.SaveChangesAsync();
        }

        private async Task<bool> SalaryExists(int id)
        {
            return await _context.Salaries.AnyAsync(e => e.Id == id);
        }
    }
} 