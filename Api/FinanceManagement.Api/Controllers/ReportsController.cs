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
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Reports/Profit
        [HttpGet("Profit")]
        public async Task<ActionResult<object>> GetProfitReport([FromQuery] int year, [FromQuery] int? month = null)
        {
            if (year <= 0)
            {
                return BadRequest("年份必须大于0");
            }

            if (month.HasValue && (month.Value <= 0 || month.Value > 12))
            {
                return BadRequest("月份必须在1-12之间");
            }

            DateTime startDate, endDate;
            string periodName;

            if (month.HasValue)
            {
                // 月度报表
                startDate = new DateTime(year, month.Value, 1);
                endDate = startDate.AddMonths(1).AddDays(-1);
                periodName = $"{year}年{month.Value}月";
            }
            else
            {
                // 年度报表
                startDate = new DateTime(year, 1, 1);
                endDate = new DateTime(year, 12, 31);
                periodName = $"{year}年";
            }

            // 获取指定时间范围内的交易记录
            var transactions = await _context.Transactions
                .Include(t => t.IncomeCategory)
                .Include(t => t.ExpenseCategory)
                .Include(t => t.Project)
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                .ToListAsync();

            // 计算总收入和总支出
            decimal totalIncome = transactions
                .Where(t => t.TransactionType == "Income")
                .Sum(t => t.Amount);

            decimal totalExpense = transactions
                .Where(t => t.TransactionType == "Expense")
                .Sum(t => t.Amount);

            // 计算利润
            decimal profit = totalIncome - totalExpense;

            // 按项目分组统计
            var projectSummary = transactions
                .GroupBy(t => new { t.ProjectId, ProjectName = t.Project.Name })
                .Select(g => new
                {
                    ProjectId = g.Key.ProjectId,
                    ProjectName = g.Key.ProjectName,
                    Income = g.Where(t => t.TransactionType == "Income").Sum(t => t.Amount),
                    Expense = g.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount),
                    Profit = g.Where(t => t.TransactionType == "Income").Sum(t => t.Amount) -
                             g.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount)
                })
                .OrderByDescending(p => p.Profit)
                .ToList();

            // 按收入科目分组统计
            var incomeCategorySummary = transactions
                .Where(t => t.TransactionType == "Income" && t.IncomeCategory != null)
                .GroupBy(t => new { t.IncomeCategoryId, CategoryName = t.IncomeCategory.Name })
                .Select(g => new
                {
                    CategoryId = g.Key.IncomeCategoryId,
                    CategoryName = g.Key.CategoryName,
                    Amount = g.Sum(t => t.Amount),
                    Percentage = g.Sum(t => t.Amount) / (totalIncome > 0 ? totalIncome : 1) * 100
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            // 按支出科目分组统计
            var expenseCategorySummary = transactions
                .Where(t => t.TransactionType == "Expense" && t.ExpenseCategory != null)
                .GroupBy(t => new { t.ExpenseCategoryId, CategoryName = t.ExpenseCategory.Name })
                .Select(g => new
                {
                    CategoryId = g.Key.ExpenseCategoryId,
                    CategoryName = g.Key.CategoryName,
                    Amount = g.Sum(t => t.Amount),
                    Percentage = g.Sum(t => t.Amount) / (totalExpense > 0 ? totalExpense : 1) * 100
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            return new
            {
                Period = periodName,
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd"),
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                Profit = profit,
                ProfitMargin = totalIncome > 0 ? profit / totalIncome * 100 : 0,
                ProjectSummary = projectSummary,
                IncomeCategorySummary = incomeCategorySummary,
                ExpenseCategorySummary = expenseCategorySummary
            };
        }

        // GET: api/Reports/Period
        [HttpGet("Period")]
        public async Task<ActionResult<object>> GetPeriodReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate > endDate)
            {
                return BadRequest("开始日期不能晚于结束日期");
            }

            // 获取指定时间范围内的交易记录
            var transactions = await _context.Transactions
                .Include(t => t.Project)
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                .ToListAsync();

            // 计算总收入和总支出
            decimal totalIncome = transactions
                .Where(t => t.TransactionType == "Income")
                .Sum(t => t.Amount);

            decimal totalExpense = transactions
                .Where(t => t.TransactionType == "Expense")
                .Sum(t => t.Amount);

            // 按日期分组统计（可按日/周/月）
            var dailySummary = transactions
                .GroupBy(t => t.TransactionDate.Date)
                .Select(g => new
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    Income = g.Where(t => t.TransactionType == "Income").Sum(t => t.Amount),
                    Expense = g.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount),
                    NetCashflow = g.Where(t => t.TransactionType == "Income").Sum(t => t.Amount) -
                                  g.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount)
                })
                .OrderBy(d => d.Date)
                .ToList();

            // 按项目分组统计
            var projectSummary = transactions
                .GroupBy(t => new { t.ProjectId, ProjectName = t.Project.Name })
                .Select(g => new
                {
                    ProjectId = g.Key.ProjectId,
                    ProjectName = g.Key.ProjectName,
                    TransactionCount = g.Count(),
                    Income = g.Where(t => t.TransactionType == "Income").Sum(t => t.Amount),
                    Expense = g.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount),
                    NetCashflow = g.Where(t => t.TransactionType == "Income").Sum(t => t.Amount) -
                                  g.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount)
                })
                .OrderByDescending(p => p.TransactionCount)
                .ToList();

            return new
            {
                Period = $"{startDate.ToString("yyyy-MM-dd")} 至 {endDate.ToString("yyyy-MM-dd")}",
                DurationDays = (endDate - startDate).Days + 1,
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                NetCashflow = totalIncome - totalExpense,
                DailySummary = dailySummary,
                ProjectSummary = projectSummary,
                TransactionCount = transactions.Count,
                AverageTransactionValue = transactions.Count > 0 
                    ? (decimal)transactions.Sum(t => t.Amount) / transactions.Count 
                    : 0
            };
        }

        // GET: api/Reports/AccountsReceivable
        [HttpGet("AccountsReceivable")]
        public async Task<ActionResult<object>> GetAccountsReceivableReport()
        {
            // 获取所有销售合同
            var salesContracts = await _context.Contracts
                .Include(c => c.Project)
                .Where(c => c.ContractType == "Sales")
                .ToListAsync();

            if (!salesContracts.Any())
            {
                return NotFound("没有销售合同数据");
            }

            // 查询所有客户
            var customers = await _context.Customers.ToListAsync();
            var customerMap = customers.ToDictionary(c => c.Id, c => c);

            // 查询所有发票并按合同ID分组
            var invoicesByContract = await _context.Invoices
                .Where(i => i.ContractId > 0)
                .GroupBy(i => i.ContractId)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(i => i.Amount));

            // 计算每个合同的应收账款
            var contractSummaries = new List<object>();
            foreach (var contract in salesContracts)
            {
                // 计算已开票金额
                decimal invoicedAmount = invoicesByContract.ContainsKey(contract.Id) 
                    ? invoicesByContract[contract.Id] 
                    : 0;
                
                // 计算应收账款（合同总金额 - 已开票金额）
                decimal receivableAmount = contract.TotalAmount - invoicedAmount;
                
                // 只包含有应收账款的合同
                if (receivableAmount > 0)
                {
                    var customer = customerMap.ContainsKey(contract.CustomerIdOrSupplierId)
                        ? customerMap[contract.CustomerIdOrSupplierId]
                        : null;

                    contractSummaries.Add(new
                    {
                        ContractId = contract.Id,
                        ContractNumber = contract.ContractNumber,
                        ProjectName = contract.Project?.Name,
                        CustomerName = customer?.Name,
                        CustomerContact = customer?.ContactPerson,
                        CustomerPhone = customer?.ContactInfo,
                        TotalAmount = contract.TotalAmount,
                        InvoicedAmount = invoicedAmount,
                        ReceivableAmount = receivableAmount,
                        CompletionRate = contract.TotalAmount > 0 
                            ? Math.Round(invoicedAmount / contract.TotalAmount * 100, 2) 
                            : 0,
                        SignDate = contract.SignDate.ToString("yyyy-MM-dd"),
                        DeliveryDate = contract.DeliveryDate?.ToString("yyyy-MM-dd"),
                        DaysOutstanding = (DateTime.Now - contract.SignDate).Days
                    });
                }
            }

            // 按客户分组汇总
            var customerSummary = contractSummaries
                .GroupBy(c => ((dynamic)c).CustomerName)
                .Select(g => new
                {
                    CustomerName = g.Key,
                    ContractCount = g.Count(),
                    TotalReceivable = g.Sum(c => ((dynamic)c).ReceivableAmount),
                    AverageCompletionRate = g.Average(c => ((dynamic)c).CompletionRate)
                })
                .OrderByDescending(c => c.TotalReceivable)
                .ToList();

            // 按账龄分析
            var agingAnalysis = contractSummaries
                .GroupBy(c => AgingCategory(((dynamic)c).DaysOutstanding))
                .Select(g => new
                {
                    AgingCategory = g.Key,
                    ContractCount = g.Count(),
                    TotalAmount = g.Sum(c => ((dynamic)c).ReceivableAmount),
                    Percentage = g.Sum(c => (decimal)((dynamic)c).ReceivableAmount) / 
                                contractSummaries.Sum(c => (decimal)((dynamic)c).ReceivableAmount) * 100
                })
                .OrderBy(a => a.AgingCategory)
                .ToList();

            return new
            {
                ReportDate = DateTime.Now.ToString("yyyy-MM-dd"),
                TotalReceivable = contractSummaries.Sum(c => (decimal)((dynamic)c).ReceivableAmount),
                ContractCount = contractSummaries.Count,
                ContractDetails = contractSummaries,
                CustomerSummary = customerSummary,
                AgingAnalysis = agingAnalysis
            };
        }

        // GET: api/Reports/AccountsPayable
        [HttpGet("AccountsPayable")]
        public async Task<ActionResult<object>> GetAccountsPayableReport()
        {
            // 获取所有采购合同
            var purchaseContracts = await _context.Contracts
                .Include(c => c.Project)
                .Where(c => c.ContractType == "Purchase")
                .ToListAsync();

            if (!purchaseContracts.Any())
            {
                return NotFound("没有采购合同数据");
            }

            // 查询所有供应商
            var suppliers = await _context.Suppliers.ToListAsync();
            var supplierMap = suppliers.ToDictionary(s => s.Id, s => s);

            // 查询所有发票并按合同ID分组
            var invoicesByContract = await _context.Invoices
                .Where(i => i.ContractId > 0)
                .GroupBy(i => i.ContractId)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(i => i.Amount));

            // 计算每个合同的应付账款
            var contractSummaries = new List<object>();
            foreach (var contract in purchaseContracts)
            {
                // 计算已开票金额
                decimal invoicedAmount = invoicesByContract.ContainsKey(contract.Id) 
                    ? invoicesByContract[contract.Id] 
                    : 0;
                
                // 计算应付账款（合同总金额 - 已开票金额）
                decimal payableAmount = contract.TotalAmount - invoicedAmount;
                
                // 只包含有应付账款的合同
                if (payableAmount > 0)
                {
                    var supplier = supplierMap.ContainsKey(contract.CustomerIdOrSupplierId)
                        ? supplierMap[contract.CustomerIdOrSupplierId]
                        : null;

                    contractSummaries.Add(new
                    {
                        ContractId = contract.Id,
                        ContractNumber = contract.ContractNumber,
                        ProjectName = contract.Project?.Name,
                        SupplierName = supplier?.Name,
                        SupplierContact = supplier?.ContactPerson,
                        SupplierPhone = supplier?.ContactInfo,
                        TotalAmount = contract.TotalAmount,
                        InvoicedAmount = invoicedAmount,
                        PayableAmount = payableAmount,
                        CompletionRate = contract.TotalAmount > 0 
                            ? Math.Round(invoicedAmount / contract.TotalAmount * 100, 2) 
                            : 0,
                        SignDate = contract.SignDate.ToString("yyyy-MM-dd"),
                        DeliveryDate = contract.DeliveryDate?.ToString("yyyy-MM-dd"),
                        DaysOutstanding = (DateTime.Now - contract.SignDate).Days
                    });
                }
            }

            // 按供应商分组汇总
            var supplierSummary = contractSummaries
                .GroupBy(c => ((dynamic)c).SupplierName)
                .Select(g => new
                {
                    SupplierName = g.Key,
                    ContractCount = g.Count(),
                    TotalPayable = g.Sum(c => ((dynamic)c).PayableAmount),
                    AverageCompletionRate = g.Average(c => ((dynamic)c).CompletionRate)
                })
                .OrderByDescending(s => s.TotalPayable)
                .ToList();

            // 按账龄分析
            var agingAnalysis = contractSummaries
                .GroupBy(c => AgingCategory(((dynamic)c).DaysOutstanding))
                .Select(g => new
                {
                    AgingCategory = g.Key,
                    ContractCount = g.Count(),
                    TotalAmount = g.Sum(c => ((dynamic)c).PayableAmount),
                    Percentage = g.Sum(c => (decimal)((dynamic)c).PayableAmount) / 
                                contractSummaries.Sum(c => (decimal)((dynamic)c).PayableAmount) * 100
                })
                .OrderBy(a => a.AgingCategory)
                .ToList();

            return new
            {
                ReportDate = DateTime.Now.ToString("yyyy-MM-dd"),
                TotalPayable = contractSummaries.Sum(c => (decimal)((dynamic)c).PayableAmount),
                ContractCount = contractSummaries.Count,
                ContractDetails = contractSummaries,
                SupplierSummary = supplierSummary,
                AgingAnalysis = agingAnalysis
            };
        }

        // GET: api/Reports/AnnualSummary/2024
        [HttpGet("AnnualSummary/{year}")]
        public async Task<ActionResult<object>> GetAnnualSummary(int year)
        {
            if (year <= 0)
            {
                return BadRequest("年份必须大于0");
            }

            // 获取指定年度的交易记录
            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year, 12, 31);
            
            var transactions = await _context.Transactions
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                .ToListAsync();

            // 按月份分组统计
            var monthlySummary = new List<object>();
            for (int month = 1; month <= 12; month++)
            {
                var monthTransactions = transactions
                    .Where(t => t.TransactionDate.Month == month)
                    .ToList();
                
                decimal monthlyIncome = monthTransactions
                    .Where(t => t.TransactionType == "Income")
                    .Sum(t => t.Amount);
                
                decimal monthlyExpense = monthTransactions
                    .Where(t => t.TransactionType == "Expense")
                    .Sum(t => t.Amount);
                
                decimal monthlyProfit = monthlyIncome - monthlyExpense;
                
                monthlySummary.Add(new
                {
                    Year = year,
                    Month = month,
                    MonthName = GetMonthName(month),
                    Income = monthlyIncome,
                    Expense = monthlyExpense,
                    Profit = monthlyProfit,
                    TransactionCount = monthTransactions.Count
                });
            }

            // 计算年度总收入和总支出
            decimal totalIncome = transactions
                .Where(t => t.TransactionType == "Income")
                .Sum(t => t.Amount);
            
            decimal totalExpense = transactions
                .Where(t => t.TransactionType == "Expense")
                .Sum(t => t.Amount);
            
            decimal annualProfit = totalIncome - totalExpense;

            // 计算季度数据
            var quarterSummary = new List<object>();
            for (int quarter = 1; quarter <= 4; quarter++)
            {
                int startMonth = (quarter - 1) * 3 + 1;
                int endMonth = quarter * 3;
                
                var quarterTransactions = transactions
                    .Where(t => t.TransactionDate.Month >= startMonth && t.TransactionDate.Month <= endMonth)
                    .ToList();
                
                decimal quarterIncome = quarterTransactions
                    .Where(t => t.TransactionType == "Income")
                    .Sum(t => t.Amount);
                
                decimal quarterExpense = quarterTransactions
                    .Where(t => t.TransactionType == "Expense")
                    .Sum(t => t.Amount);
                
                decimal quarterProfit = quarterIncome - quarterExpense;
                
                quarterSummary.Add(new
                {
                    Year = year,
                    Quarter = quarter,
                    QuarterName = $"Q{quarter}",
                    Income = quarterIncome,
                    Expense = quarterExpense,
                    Profit = quarterProfit,
                    IncomePercentage = totalIncome > 0 ? quarterIncome / totalIncome * 100 : 0,
                    ExpensePercentage = totalExpense > 0 ? quarterExpense / totalExpense * 100 : 0,
                    TransactionCount = quarterTransactions.Count
                });
            }

            return new
            {
                Year = year,
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                AnnualProfit = annualProfit,
                ProfitMargin = totalIncome > 0 ? annualProfit / totalIncome * 100 : 0,
                TransactionCount = transactions.Count,
                MonthlySummary = monthlySummary,
                QuarterSummary = quarterSummary,
                TopMonthByIncome = monthlySummary.OrderByDescending(m => ((dynamic)m).Income).FirstOrDefault(),
                TopMonthByProfit = monthlySummary.OrderByDescending(m => ((dynamic)m).Profit).FirstOrDefault()
            };
        }

        #region 辅助方法

        private string AgingCategory(int days)
        {
            if (days <= 30) return "30天内";
            if (days <= 60) return "31-60天";
            if (days <= 90) return "61-90天";
            if (days <= 180) return "91-180天";
            if (days <= 365) return "181-365天";
            return "365天以上";
        }

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

        #endregion

        #region 报表下载功能

        // GET: api/Reports/Profit/Export
        [HttpGet("Profit/Export")]
        public async Task<IActionResult> ExportProfitReport([FromQuery] int year, [FromQuery] int? month = null, [FromQuery] string format = "excel")
        {
            if (year <= 0)
            {
                return BadRequest("年份必须大于0");
            }

            if (month.HasValue && (month.Value <= 0 || month.Value > 12))
            {
                return BadRequest("月份必须在1-12之间");
            }

            // 获取报表数据
            var profitReport = await GetProfitReport(year, month);
            
            if (profitReport.Result is BadRequestObjectResult)
            {
                return profitReport.Result;
            }

            var reportData = (profitReport.Value as dynamic);
            
            // 根据请求的格式导出报表
            if (format.ToLower() == "pdf")
            {
                // 生成PDF格式报表
                byte[] pdfBytes = GenerateProfitReportPdf(reportData);
                string fileName = $"利润报表_{reportData.Period}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            else // 默认为Excel格式
            {
                // 生成Excel格式报表
                byte[] excelBytes = GenerateProfitReportExcel(reportData);
                string fileName = $"利润报表_{reportData.Period}.xlsx";
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // GET: api/Reports/Period/Export
        [HttpGet("Period/Export")]
        public async Task<IActionResult> ExportPeriodReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string format = "excel")
        {
            if (startDate > endDate)
            {
                return BadRequest("开始日期不能晚于结束日期");
            }

            // 获取报表数据
            var periodReport = await GetPeriodReport(startDate, endDate);
            
            if (periodReport.Result is BadRequestObjectResult)
            {
                return periodReport.Result;
            }

            var reportData = (periodReport.Value as dynamic);
            
            // 根据请求的格式导出报表
            if (format.ToLower() == "pdf")
            {
                // 生成PDF格式报表
                byte[] pdfBytes = GeneratePeriodReportPdf(reportData);
                string fileName = $"期间报表_{startDate:yyyy-MM-dd}_至_{endDate:yyyy-MM-dd}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            else // 默认为Excel格式
            {
                // 生成Excel格式报表
                byte[] excelBytes = GeneratePeriodReportExcel(reportData);
                string fileName = $"期间报表_{startDate:yyyy-MM-dd}_至_{endDate:yyyy-MM-dd}.xlsx";
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // GET: api/Reports/AccountsReceivable/Export
        [HttpGet("AccountsReceivable/Export")]
        public async Task<IActionResult> ExportAccountsReceivableReport([FromQuery] string format = "excel")
        {
            // 获取报表数据
            var receivableReport = await GetAccountsReceivableReport();
            
            if (receivableReport.Result is NotFoundObjectResult)
            {
                return receivableReport.Result;
            }

            var reportData = (receivableReport.Value as dynamic);
            
            // 根据请求的格式导出报表
            if (format.ToLower() == "pdf")
            {
                // 生成PDF格式报表
                byte[] pdfBytes = GenerateReceivableReportPdf(reportData);
                string fileName = $"应收账款报表_{DateTime.Now:yyyy-MM-dd}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            else // 默认为Excel格式
            {
                // 生成Excel格式报表
                byte[] excelBytes = GenerateReceivableReportExcel(reportData);
                string fileName = $"应收账款报表_{DateTime.Now:yyyy-MM-dd}.xlsx";
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // GET: api/Reports/AccountsPayable/Export
        [HttpGet("AccountsPayable/Export")]
        public async Task<IActionResult> ExportAccountsPayableReport([FromQuery] string format = "excel")
        {
            // 获取报表数据
            var payableReport = await GetAccountsPayableReport();
            
            if (payableReport.Result is NotFoundObjectResult)
            {
                return payableReport.Result;
            }

            var reportData = (payableReport.Value as dynamic);
            
            // 根据请求的格式导出报表
            if (format.ToLower() == "pdf")
            {
                // 生成PDF格式报表
                byte[] pdfBytes = GeneratePayableReportPdf(reportData);
                string fileName = $"应付账款报表_{DateTime.Now:yyyy-MM-dd}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            else // 默认为Excel格式
            {
                // 生成Excel格式报表
                byte[] excelBytes = GeneratePayableReportExcel(reportData);
                string fileName = $"应付账款报表_{DateTime.Now:yyyy-MM-dd}.xlsx";
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // GET: api/Reports/AnnualSummary/Export/{year}
        [HttpGet("AnnualSummary/Export/{year}")]
        public async Task<IActionResult> ExportAnnualSummaryReport(int year, [FromQuery] string format = "excel")
        {
            if (year <= 0)
            {
                return BadRequest("年份必须大于0");
            }

            // 获取报表数据
            var annualReport = await GetAnnualSummary(year);
            
            if (annualReport.Result is BadRequestObjectResult)
            {
                return annualReport.Result;
            }

            var reportData = (annualReport.Value as dynamic);
            
            // 根据请求的格式导出报表
            if (format.ToLower() == "pdf")
            {
                // 生成PDF格式报表
                byte[] pdfBytes = GenerateAnnualSummaryPdf(reportData);
                string fileName = $"年度汇总报表_{year}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            else // 默认为Excel格式
            {
                // 生成Excel格式报表
                byte[] excelBytes = GenerateAnnualSummaryExcel(reportData);
                string fileName = $"年度汇总报表_{year}.xlsx";
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        #region PDF生成方法

        private byte[] GenerateProfitReportPdf(dynamic reportData)
        {
            // 这里应该使用PDF生成库，如iTextSharp或PdfSharp来创建PDF文件
            // 以下是示例代码，实际项目中需要替换为真实的PDF生成逻辑
            // 由于篇幅限制，只展示示例代码结构

            // TODO: 实现实际的PDF生成逻辑
            using (var memoryStream = new System.IO.MemoryStream())
            {
                // 使用PDF生成库创建PDF文件
                // 例如：
                // Document document = new Document(PageSize.A4);
                // PdfWriter.GetInstance(document, memoryStream);
                // document.Open();
                // 添加标题、表格等内容...
                // document.Close();

                // 目前为了示例，返回一个空的PDF字节数组
                return memoryStream.ToArray();
            }
        }

        private byte[] GeneratePeriodReportPdf(dynamic reportData)
        {
            // PDF生成逻辑，类似于GenerateProfitReportPdf
            using (var memoryStream = new System.IO.MemoryStream())
            {
                // TODO: 实现实际的PDF生成逻辑
                return memoryStream.ToArray();
            }
        }

        private byte[] GenerateReceivableReportPdf(dynamic reportData)
        {
            // PDF生成逻辑，类似于GenerateProfitReportPdf
            using (var memoryStream = new System.IO.MemoryStream())
            {
                // TODO: 实现实际的PDF生成逻辑
                return memoryStream.ToArray();
            }
        }

        private byte[] GeneratePayableReportPdf(dynamic reportData)
        {
            // PDF生成逻辑，类似于GenerateProfitReportPdf
            using (var memoryStream = new System.IO.MemoryStream())
            {
                // TODO: 实现实际的PDF生成逻辑
                return memoryStream.ToArray();
            }
        }

        private byte[] GenerateAnnualSummaryPdf(dynamic reportData)
        {
            // PDF生成逻辑，类似于GenerateProfitReportPdf
            using (var memoryStream = new System.IO.MemoryStream())
            {
                // TODO: 实现实际的PDF生成逻辑
                return memoryStream.ToArray();
            }
        }

        #endregion

        #region Excel生成方法

        private byte[] GenerateProfitReportExcel(dynamic reportData)
        {
            // 这里应该使用Excel生成库，如EPPlus或NPOI来创建Excel文件
            // 以下是示例代码，实际项目中需要替换为真实的Excel生成逻辑
            // 由于篇幅限制，只展示示例代码结构

            // TODO: 实现实际的Excel生成逻辑
            using (var memoryStream = new System.IO.MemoryStream())
            {
                // 使用Excel生成库创建Excel文件
                // 例如：
                // using (var package = new ExcelPackage(memoryStream))
                // {
                //     var worksheet = package.Workbook.Worksheets.Add("利润报表");
                //     // 添加标题行
                //     worksheet.Cells[1, 1].Value = "利润报表";
                //     worksheet.Cells[2, 1].Value = "期间";
                //     worksheet.Cells[2, 2].Value = reportData.Period;
                //     // 添加数据...
                //     package.Save();
                // }

                // 目前为了示例，返回一个空的Excel字节数组
                return memoryStream.ToArray();
            }
        }

        private byte[] GeneratePeriodReportExcel(dynamic reportData)
        {
            // Excel生成逻辑，类似于GenerateProfitReportExcel
            using (var memoryStream = new System.IO.MemoryStream())
            {
                // TODO: 实现实际的Excel生成逻辑
                return memoryStream.ToArray();
            }
        }

        private byte[] GenerateReceivableReportExcel(dynamic reportData)
        {
            // Excel生成逻辑，类似于GenerateProfitReportExcel
            using (var memoryStream = new System.IO.MemoryStream())
            {
                // TODO: 实现实际的Excel生成逻辑
                return memoryStream.ToArray();
            }
        }

        private byte[] GeneratePayableReportExcel(dynamic reportData)
        {
            // Excel生成逻辑，类似于GenerateProfitReportExcel
            using (var memoryStream = new System.IO.MemoryStream())
            {
                // TODO: 实现实际的Excel生成逻辑
                return memoryStream.ToArray();
            }
        }

        private byte[] GenerateAnnualSummaryExcel(dynamic reportData)
        {
            // Excel生成逻辑，类似于GenerateProfitReportExcel
            using (var memoryStream = new System.IO.MemoryStream())
            {
                // TODO: 实现实际的Excel生成逻辑
                return memoryStream.ToArray();
            }
        }

        #endregion

        #endregion
    }
} 