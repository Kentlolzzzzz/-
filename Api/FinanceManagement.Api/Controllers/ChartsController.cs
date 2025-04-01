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
    public class ChartsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChartsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Charts/income-expense
        [HttpGet("income-expense")]
        public async Task<ActionResult<object>> GetIncomeExpenseChart([FromQuery] int? year)
        {
            var currentYear = DateTime.Now.Year;
            int targetYear = year ?? currentYear;

            // 查询指定年度的交易数据
            var transactions = await _context.Transactions
                .Where(t => t.TransactionDate.Year == targetYear)
                .ToListAsync();

            // 按月份分组统计收入和支出
            var monthlyData = new List<object>();
            for (int month = 1; month <= 12; month++)
            {
                var monthTransactions = transactions.Where(t => t.TransactionDate.Month == month);
                var income = monthTransactions.Where(t => t.TransactionType == "Income").Sum(t => t.Amount);
                var expense = monthTransactions.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount);
                var profit = income - expense;

                monthlyData.Add(new
                {
                    Month = month,
                    MonthName = GetMonthName(month),
                    Income = income,
                    Expense = expense,
                    Profit = profit
                });
            }

            // 计算总计
            var totalIncome = transactions.Where(t => t.TransactionType == "Income").Sum(t => t.Amount);
            var totalExpense = transactions.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount);
            var totalProfit = totalIncome - totalExpense;

            return new
            {
                Year = targetYear,
                MonthlyData = monthlyData,
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                TotalProfit = totalProfit,
                ProfitMargin = totalIncome > 0 ? decimal.Round(totalProfit / totalIncome * 100, 2) : 0
            };
        }

        // GET: api/Charts/income-categories
        [HttpGet("income-categories")]
        public async Task<ActionResult<object>> GetIncomeCategoriesChart([FromQuery] int? year, [FromQuery] int? month)
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
                .Include(t => t.IncomeCategory)
                .ToListAsync();

            // 计算总收入
            decimal totalIncome = transactions.Sum(t => t.Amount);

            // 按收入类别分组
            var categoryData = transactions
                .GroupBy(t => t.IncomeCategory?.Name ?? "未分类")
                .Select(g => new
                {
                    Category = g.Key,
                    Amount = g.Sum(t => t.Amount),
                    Percentage = totalIncome > 0 ? decimal.Round(g.Sum(t => t.Amount) / totalIncome * 100, 2) : 0,
                    TransactionCount = g.Count()
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            return new
            {
                Period = GetPeriodDescription(year, month),
                TotalIncome = totalIncome,
                CategoryCount = categoryData.Count,
                Categories = categoryData,
                TopCategories = categoryData.Take(5).ToList(),
                OtherCategories = categoryData.Count > 5 ? 
                    new 
                    { 
                        Category = "其他", 
                        Amount = categoryData.Skip(5).Sum(c => c.Amount),
                        Percentage = totalIncome > 0 ? decimal.Round(categoryData.Skip(5).Sum(c => c.Amount) / totalIncome * 100, 2) : 0,
                        TransactionCount = categoryData.Skip(5).Sum(c => c.TransactionCount)
                    } : null
            };
        }

        // GET: api/Charts/expense-categories
        [HttpGet("expense-categories")]
        public async Task<ActionResult<object>> GetExpenseCategoriesChart([FromQuery] int? year, [FromQuery] int? month)
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
                .Include(t => t.ExpenseCategory)
                .ToListAsync();

            // 计算总支出
            decimal totalExpense = transactions.Sum(t => t.Amount);

            // 按支出类别分组
            var categoryData = transactions
                .GroupBy(t => t.ExpenseCategory?.Name ?? "未分类")
                .Select(g => new
                {
                    Category = g.Key,
                    Amount = g.Sum(t => t.Amount),
                    Percentage = totalExpense > 0 ? decimal.Round(g.Sum(t => t.Amount) / totalExpense * 100, 2) : 0,
                    TransactionCount = g.Count()
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            return new
            {
                Period = GetPeriodDescription(year, month),
                TotalExpense = totalExpense,
                CategoryCount = categoryData.Count,
                Categories = categoryData,
                TopCategories = categoryData.Take(5).ToList(),
                OtherCategories = categoryData.Count > 5 ? 
                    new 
                    { 
                        Category = "其他", 
                        Amount = categoryData.Skip(5).Sum(c => c.Amount),
                        Percentage = totalExpense > 0 ? decimal.Round(categoryData.Skip(5).Sum(c => c.Amount) / totalExpense * 100, 2) : 0,
                        TransactionCount = categoryData.Skip(5).Sum(c => c.TransactionCount)
                    } : null
            };
        }

        // GET: api/Charts/monthly-trend
        [HttpGet("monthly-trend")]
        public async Task<ActionResult<object>> GetMonthlyTrendChart([FromQuery] int months = 12)
        {
            // 限制最多查询36个月
            if (months > 36) months = 36;
            
            // 计算开始日期
            var endDate = DateTime.Now;
            var startDate = endDate.AddMonths(-months + 1).Date.AddDays(-(endDate.Day - 1)); // 从指定月份的1号开始

            // 查询交易记录
            var transactions = await _context.Transactions
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                .ToListAsync();

            // 按月份分组
            var monthlyData = new List<object>();
            for (int i = 0; i < months; i++)
            {
                var currentMonth = startDate.AddMonths(i);
                var monthTransactions = transactions.Where(t => 
                    t.TransactionDate.Year == currentMonth.Year && 
                    t.TransactionDate.Month == currentMonth.Month);
                
                var income = monthTransactions.Where(t => t.TransactionType == "Income").Sum(t => t.Amount);
                var expense = monthTransactions.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount);
                var balance = income - expense;
                
                monthlyData.Add(new
                {
                    Year = currentMonth.Year,
                    Month = currentMonth.Month,
                    MonthName = GetMonthName(currentMonth.Month),
                    Period = $"{currentMonth.Year}-{currentMonth.Month:D2}",
                    Income = income,
                    Expense = expense,
                    Balance = balance,
                    GrowthRate = i > 0 ? CalculateGrowthRate(
                        monthlyData[i - 1].GetType().GetProperty("Income").GetValue(monthlyData[i - 1], null),
                        income) : 0
                });
            }

            return new
            {
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd"),
                MonthCount = months,
                MonthlyData = monthlyData,
                TotalIncome = transactions.Where(t => t.TransactionType == "Income").Sum(t => t.Amount),
                TotalExpense = transactions.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount),
                TotalBalance = transactions.Where(t => t.TransactionType == "Income").Sum(t => t.Amount) - 
                             transactions.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount)
            };
        }

        // GET: api/Charts/project-profit
        [HttpGet("project-profit")]
        public async Task<ActionResult<object>> GetProjectProfitChart([FromQuery] int? year, [FromQuery] int top = 10)
        {
            var query = _context.Transactions.AsQueryable();
            
            // 应用年份筛选
            if (year.HasValue)
            {
                query = query.Where(t => t.TransactionDate.Year == year.Value);
            }

            var transactions = await query
                .Include(t => t.Project)
                .ToListAsync();

            // 按项目分组
            var projectData = transactions
                .Where(t => t.Project != null)
                .GroupBy(t => new { t.ProjectId, ProjectName = t.Project.Name })
                .Select(g => new
                {
                    ProjectId = g.Key.ProjectId,
                    ProjectName = g.Key.ProjectName,
                    Income = g.Where(t => t.TransactionType == "Income").Sum(t => t.Amount),
                    Expense = g.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount),
                    Profit = g.Where(t => t.TransactionType == "Income").Sum(t => t.Amount) - 
                           g.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(p => p.Profit)
                .Take(top)
                .ToList();

            // 计算总利润
            decimal totalProfit = projectData.Sum(p => p.Profit);

            return new
            {
                Year = year,
                TopProjectCount = top,
                Projects = projectData,
                TotalProfit = totalProfit,
                MostProfitableProject = projectData.FirstOrDefault(),
                LeastProfitableProject = projectData.OrderBy(p => p.Profit).FirstOrDefault()
            };
        }

        // GET: api/Charts/employee-performance
        [HttpGet("employee-performance")]
        public async Task<ActionResult<object>> GetEmployeePerformanceChart([FromQuery] int? year, [FromQuery] int? month)
        {
            // 获取员工数据
            var employees = await _context.Employees
                .Where(e => e.Status == "Active")
                .ToListAsync();
                
            // 获取工资数据
            var query = _context.Salaries.AsQueryable();
            
            // 应用时间筛选
            if (year.HasValue)
            {
                query = query.Where(s => s.SalaryMonth.Year == year.Value);
                
                if (month.HasValue)
                {
                    query = query.Where(s => s.SalaryMonth.Month == month.Value);
                }
            }
            
            var salaries = await query
                .Include(s => s.Employee)
                .ToListAsync();
                
            // 获取考勤数据
            var attendanceQuery = _context.Attendances.AsQueryable();
            
            if (year.HasValue)
            {
                attendanceQuery = attendanceQuery.Where(a => a.AttendanceDate.Year == year.Value);
                
                if (month.HasValue)
                {
                    attendanceQuery = attendanceQuery.Where(a => a.AttendanceDate.Month == month.Value);
                }
            }
            
            var attendances = await attendanceQuery
                .Include(a => a.Employee)
                .ToListAsync();

            // 按员工计算绩效指标
            var employeePerformance = employees
                .Select(e => new
                {
                    EmployeeId = e.Id,
                    EmployeeName = e.Name,
                    Department = e.Department,
                    Position = e.Position,
                    BaseSalary = e.BaseSalary,
                    // 工资数据
                    SalaryData = salaries
                        .Where(s => s.EmployeeId == e.Id)
                        .Select(s => new 
                        { 
                            s.SalaryMonth, 
                            s.BasicSalary, 
                            s.Bonus, 
                            s.Deduction, 
                            s.ActualPayment 
                        })
                        .OrderBy(s => s.SalaryMonth)
                        .ToList(),
                    // 考勤数据
                    AttendanceData = attendances
                        .Where(a => a.EmployeeId == e.Id)
                        .GroupBy(a => a.Status)
                        .Select(g => new { Status = g.Key, Count = g.Count() })
                        .ToList(),
                    // 计算关键绩效指标
                    TotalWorkDays = attendances.Count(a => a.EmployeeId == e.Id),
                    TotalWorkHours = attendances.Where(a => a.EmployeeId == e.Id).Sum(a => a.WorkHours),
                    AbsenceDays = attendances.Count(a => a.EmployeeId == e.Id && a.Status == "Absent"),
                    AverageBonus = salaries.Where(s => s.EmployeeId == e.Id).Average(s => s.Bonus + 0), // 防止没有数据时出错
                    PerformanceScore = CalculatePerformanceScore(
                        attendances.Where(a => a.EmployeeId == e.Id).ToList(),
                        salaries.Where(s => s.EmployeeId == e.Id).ToList()
                    )
                })
                .OrderByDescending(e => e.PerformanceScore)
                .ToList();

            return new
            {
                Period = GetPeriodDescription(year, month),
                EmployeeCount = employeePerformance.Count,
                TopPerformers = employeePerformance.Take(5).ToList(),
                DepartmentPerformance = employeePerformance
                    .GroupBy(e => e.Department)
                    .Select(g => new 
                    { 
                        Department = g.Key, 
                        AverageScore = g.Average(e => e.PerformanceScore),
                        EmployeeCount = g.Count(), 
                        TopPerformer = g.OrderByDescending(e => e.PerformanceScore).FirstOrDefault() 
                    })
                    .OrderByDescending(d => d.AverageScore)
                    .ToList(),
                PositionPerformance = employeePerformance
                    .GroupBy(e => e.Position)
                    .Select(g => new 
                    { 
                        Position = g.Key, 
                        AverageScore = g.Average(e => e.PerformanceScore),
                        EmployeeCount = g.Count() 
                    })
                    .OrderByDescending(p => p.AverageScore)
                    .ToList()
            };
        }

        #region 辅助方法

        private string GetMonthName(int month)
        {
            return month switch
            {
                1 => "一月",
                2 => "二月",
                3 => "三月",
                4 => "四月",
                5 => "五月",
                6 => "六月",
                7 => "七月",
                8 => "八月",
                9 => "九月",
                10 => "十月",
                11 => "十一月",
                12 => "十二月",
                _ => string.Empty
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

        private decimal CalculateGrowthRate(object previousValue, decimal currentValue)
        {
            if (previousValue == null) return 0;
            
            decimal previous = Convert.ToDecimal(previousValue);
            if (previous == 0) return 0;
            
            return decimal.Round((currentValue - previous) / Math.Abs(previous) * 100, 2);
        }

        private decimal CalculatePerformanceScore(List<Attendance> attendances, List<Salary> salaries)
        {
            // 这是一个简化的绩效计算示例，实际应根据业务需求定制
            if (attendances.Count == 0 || salaries.Count == 0) return 0;
            
            // 考勤分数 (满勤为60分)
            int totalDays = attendances.Count;
            int absentDays = attendances.Count(a => a.Status == "Absent");
            int lateDays = attendances.Count(a => a.Status == "Late");
            decimal attendanceScore = totalDays > 0 ? 60 * (1 - 0.5m * absentDays / totalDays - 0.2m * lateDays / totalDays) : 0;
            
            // 奖金分数 (奖金占基本工资比例，最高40分)
            decimal totalBasicSalary = salaries.Sum(s => s.BasicSalary);
            decimal totalBonus = salaries.Sum(s => s.Bonus);
            decimal bonusScore = totalBasicSalary > 0 ? Math.Min(40 * totalBonus / totalBasicSalary, 40) : 0;
            
            return decimal.Round(attendanceScore + bonusScore, 2);
        }

        #endregion
    }
}