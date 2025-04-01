using FinanceManagement.Api.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FinanceManagement.Api.Data
{
    public static class SeedData
    {
        public static void Initialize(ApplicationDbContext context)
        {
            if (!context.Users.Any())
            {
                // 添加用户
                var users = new List<User>
                {
                    new User { Username = "admin", PasswordHash = HashPassword("admin123"), Role = "Admin", RealName = "系统管理员", IsActive = true },
                    new User { Username = "user1", PasswordHash = HashPassword("user123"), Role = "User", RealName = "普通用户", IsActive = true },
                    new User { Username = "finance", PasswordHash = HashPassword("finance123"), Role = "Finance", RealName = "财务人员", IsActive = true }
                };
                context.Users.AddRange(users);
                context.SaveChanges();
                
                // 添加项目
                var projects = new List<Project>
                {
                    new Project { Name = "办公室装修", Code = "PRJ001", Description = "公司办公室装修项目", Budget = 100000, StartDate = DateTime.Now.AddMonths(-2), EndDate = DateTime.Now.AddMonths(4), Status = "进行中", ManagerId = 1 },
                    new Project { Name = "软件开发", Code = "PRJ002", Description = "ERP系统开发", Budget = 200000, StartDate = DateTime.Now.AddMonths(-3), EndDate = DateTime.Now.AddMonths(3), Status = "进行中", ManagerId = 1 },
                    new Project { Name = "市场推广", Code = "PRJ003", Description = "新产品市场推广活动", Budget = 50000, StartDate = DateTime.Now.AddDays(-15), EndDate = DateTime.Now.AddMonths(2), Status = "准备中", ManagerId = 2 }
                };
                context.Projects.AddRange(projects);
                context.SaveChanges();
                
                // 添加收入类别
                var incomeCategories = new List<IncomeCategory>
                {
                    new IncomeCategory { Name = "销售收入", Code = "IC001", Description = "产品销售产生的收入" },
                    new IncomeCategory { Name = "服务收入", Code = "IC002", Description = "提供服务产生的收入" },
                    new IncomeCategory { Name = "利息收入", Code = "IC003", Description = "银行存款利息" },
                    new IncomeCategory { Name = "投资收益", Code = "IC004", Description = "投资产生的收益" }
                };
                context.IncomeCategories.AddRange(incomeCategories);
                context.SaveChanges();
                
                // 添加支出类别
                var expenseCategories = new List<ExpenseCategory>
                {
                    new ExpenseCategory { Name = "办公用品", Code = "EC001", Description = "办公用品采购支出" },
                    new ExpenseCategory { Name = "水电费", Code = "EC002", Description = "水电费支出" },
                    new ExpenseCategory { Name = "房租", Code = "EC003", Description = "办公室租金" },
                    new ExpenseCategory { Name = "员工薪资", Code = "EC004", Description = "员工工资支出" },
                    new ExpenseCategory { Name = "差旅费", Code = "EC005", Description = "员工出差费用" }
                };
                context.ExpenseCategories.AddRange(expenseCategories);
                context.SaveChanges();
                
                // 添加发票类型
                var invoiceTypes = new List<InvoiceType>
                {
                    new InvoiceType { Name = "普通发票", Code = "IT001", Description = "普通增值税发票" },
                    new InvoiceType { Name = "专用发票", Code = "IT002", Description = "增值税专用发票" },
                    new InvoiceType { Name = "电子发票", Code = "IT003", Description = "电子增值税发票" }
                };
                context.InvoiceTypes.AddRange(invoiceTypes);
                context.SaveChanges();
                
                // 添加税率
                var taxRates = new List<TaxRate>
                {
                    new TaxRate { Rate = 0.03m, Description = "3%增值税" },
                    new TaxRate { Rate = 0.06m, Description = "6%增值税" },
                    new TaxRate { Rate = 0.09m, Description = "9%增值税" },
                    new TaxRate { Rate = 0.13m, Description = "13%增值税" }
                };
                context.TaxRates.AddRange(taxRates);
                context.SaveChanges();
                
                // 添加账户
                var accounts = new List<Account>
                {
                    new Account { Name = "现金账户", AccountNumber = "CASH001", Balance = 50000, AccountType = "现金", IsActive = true },
                    new Account { Name = "工商银行", AccountNumber = "ICBC12345678", Balance = 200000, AccountType = "银行卡", IsActive = true },
                    new Account { Name = "支付宝", AccountNumber = "ALIPAY001", Balance = 30000, AccountType = "第三方支付", IsActive = true }
                };
                context.Accounts.AddRange(accounts);
                context.SaveChanges();
                
                // 添加客户
                var customers = new List<Customer>
                {
                    new Customer { Name = "上海ABC科技有限公司", ContactPerson = "张先生", ContactInfo = "13812345678", Address = "上海市浦东新区张江高科技园区", TaxId = "91310000XXXXXXXX", CreditLimit = 200000 },
                    new Customer { Name = "北京XYZ贸易有限公司", ContactPerson = "李女士", ContactInfo = "13987654321", Address = "北京市海淀区中关村", TaxId = "91110000XXXXXXXX", CreditLimit = 150000 },
                    new Customer { Name = "广州未来科技有限公司", ContactPerson = "王总", ContactInfo = "13567891234", Address = "广州市天河区珠江新城", TaxId = "91440000XXXXXXXX", CreditLimit = 100000 }
                };
                context.Customers.AddRange(customers);
                context.SaveChanges();
                
                // 添加供应商
                var suppliers = new List<Supplier>
                {
                    new Supplier { Name = "广州办公用品有限公司", ContactPerson = "郑经理", ContactInfo = "18912345678", Address = "广州市白云区机场路", TaxId = "91440000YYYYYYYY", BankAccount = "62220000000001" },
                    new Supplier { Name = "深圳电子科技有限公司", ContactPerson = "陈工", ContactInfo = "18887654321", Address = "深圳市南山区科技园", TaxId = "91440300YYYYYYYY", BankAccount = "62220000000002" },
                    new Supplier { Name = "杭州软件服务有限公司", ContactPerson = "徐总", ContactInfo = "18765432109", Address = "杭州市滨江区网商路", TaxId = "91330000YYYYYYYY", BankAccount = "62220000000003" }
                };
                context.Suppliers.AddRange(suppliers);
                context.SaveChanges();
                
                // 添加员工
                var employees = new List<Employee>
                {
                    new Employee { Name = "张三", EmployeeNumber = "EMP001", Position = "销售经理", Department = "销售部", HireDate = DateTime.Now.AddYears(-2), BaseSalary = 10000, Status = "在职", ContactInfo = "13812345678" },
                    new Employee { Name = "李四", EmployeeNumber = "EMP002", Position = "技术总监", Department = "技术部", HireDate = DateTime.Now.AddYears(-3), BaseSalary = 15000, Status = "在职", ContactInfo = "13987654321" },
                    new Employee { Name = "王五", EmployeeNumber = "EMP003", Position = "财务主管", Department = "财务部", HireDate = DateTime.Now.AddYears(-1), BaseSalary = 12000, Status = "在职", ContactInfo = "13567891234" },
                    new Employee { Name = "赵六", EmployeeNumber = "EMP004", Position = "人事专员", Department = "人力资源部", HireDate = DateTime.Now.AddMonths(-6), BaseSalary = 8000, Status = "在职", ContactInfo = "13612345678" }
                };
                context.Employees.AddRange(employees);
                context.SaveChanges();
                
                // 添加考勤记录
                var now = DateTime.Now;
                var attendances = new List<Attendance>
                {
                    new Attendance { EmployeeId = 1, AttendanceDate = now.AddDays(-1), Status = "正常", CheckIn = new TimeSpan(9, 0, 0), CheckOut = new TimeSpan(18, 0, 0), WorkHours = 8, Remarks = "正常出勤" },
                    new Attendance { EmployeeId = 2, AttendanceDate = now.AddDays(-1), Status = "正常", CheckIn = new TimeSpan(8, 50, 0), CheckOut = new TimeSpan(18, 10, 0), WorkHours = 8, Remarks = "正常出勤" },
                    new Attendance { EmployeeId = 3, AttendanceDate = now.AddDays(-1), Status = "正常", CheckIn = new TimeSpan(9, 0, 0), CheckOut = new TimeSpan(18, 0, 0), WorkHours = 8, Remarks = "正常出勤" },
                    new Attendance { EmployeeId = 4, AttendanceDate = now.AddDays(-1), Status = "正常", CheckIn = new TimeSpan(8, 45, 0), CheckOut = new TimeSpan(17, 50, 0), WorkHours = 8, Remarks = "正常出勤" },
                    new Attendance { EmployeeId = 1, AttendanceDate = now.AddDays(-2), Status = "正常", CheckIn = new TimeSpan(9, 0, 0), CheckOut = new TimeSpan(18, 0, 0), WorkHours = 8, Remarks = "正常出勤" },
                    new Attendance { EmployeeId = 2, AttendanceDate = now.AddDays(-2), Status = "迟到", CheckIn = new TimeSpan(9, 30, 0), CheckOut = new TimeSpan(18, 0, 0), WorkHours = 7.5m, Remarks = "迟到30分钟" },
                    new Attendance { EmployeeId = 3, AttendanceDate = now.AddDays(-2), Status = "正常", CheckIn = new TimeSpan(8, 55, 0), CheckOut = new TimeSpan(18, 5, 0), WorkHours = 8, Remarks = "正常出勤" },
                    new Attendance { EmployeeId = 4, AttendanceDate = now.AddDays(-2), Status = "请假", CheckIn = new TimeSpan(9, 0, 0), CheckOut = new TimeSpan(13, 0, 0), WorkHours = 4, Remarks = "下午请假" }
                };
                context.Attendances.AddRange(attendances);
                context.SaveChanges();
                
                // 添加薪资记录
                var lastMonth = new DateTime(now.Year, now.Month > 1 ? now.Month - 1 : 12, 1);
                var salaries = new List<Salary>
                {
                    new Salary { EmployeeId = 1, SalaryMonth = lastMonth, BasicSalary = 10000, Bonus = 2000, Deduction = 0, ActualPayment = 12000, PaymentStatus = "已发放", PaymentDate = lastMonth.AddDays(25) },
                    new Salary { EmployeeId = 2, SalaryMonth = lastMonth, BasicSalary = 15000, Bonus = 3000, Deduction = 500, ActualPayment = 17500, PaymentStatus = "已发放", PaymentDate = lastMonth.AddDays(25) },
                    new Salary { EmployeeId = 3, SalaryMonth = lastMonth, BasicSalary = 12000, Bonus = 1500, Deduction = 0, ActualPayment = 13500, PaymentStatus = "已发放", PaymentDate = lastMonth.AddDays(25) },
                    new Salary { EmployeeId = 4, SalaryMonth = lastMonth, BasicSalary = 8000, Bonus = 1000, Deduction = 200, ActualPayment = 8800, PaymentStatus = "已发放", PaymentDate = lastMonth.AddDays(25) }
                };
                context.Salaries.AddRange(salaries);
                context.SaveChanges();
                
                // 添加贷款记录
                var loans = new List<Loan>
                {
                    new Loan { EmployeeId = 2, LoanAmount = 50000, LoanDate = now.AddMonths(-6), LoanPurpose = "购房首付", RepaymentMonths = 12, MonthlyPayment = 4250, Status = "还款中" },
                    new Loan { EmployeeId = 4, LoanAmount = 10000, LoanDate = now.AddMonths(-2), LoanPurpose = "个人消费", RepaymentMonths = 10, MonthlyPayment = 1020, Status = "还款中" }
                };
                context.Loans.AddRange(loans);
                context.SaveChanges();
                
                // 添加合同
                var contracts = new List<Contract>
                {
                    new Contract { 
                        ProjectId = 1, 
                        ContractNumber = "HT202401001", 
                        ContractType = "采购", 
                        CustomerIdOrSupplierId = 1, // 供应商ID 
                        SignDate = now.AddMonths(-2), 
                        TotalAmount = 100000, 
                        Status = "执行中", 
                        DeliveryDate = now.AddMonths(4), 
                        PaymentTerms = "分期付款", 
                        TaxRateId = 2
                    },
                    new Contract { 
                        ProjectId = 2, 
                        ContractNumber = "HT202401002", 
                        ContractType = "采购", 
                        CustomerIdOrSupplierId = 3, // 供应商ID 
                        SignDate = now.AddMonths(-3), 
                        TotalAmount = 200000, 
                        Status = "执行中", 
                        DeliveryDate = now.AddMonths(3), 
                        PaymentTerms = "分期付款", 
                        TaxRateId = 2
                    }
                };
                context.Contracts.AddRange(contracts);
                context.SaveChanges();
                
                // 添加发票
                var invoices = new List<Invoice>
                {
                    new Invoice { 
                        ContractId = 1, 
                        InvoiceNumber = "FP202401001", 
                        InvoiceDate = now.AddMonths(-1), 
                        Amount = 30000, 
                        InvoiceTypeId = 2, 
                        TaxRateId = 2, 
                        TaxAmount = 1800, // 6%
                        AmountWithoutTax = 28200,
                        Status = "已开票", 
                        InvoiceType_ = "采购"
                    },
                    new Invoice { 
                        ContractId = 2, 
                        InvoiceNumber = "FP202401002", 
                        InvoiceDate = now.AddMonths(-2), 
                        Amount = 60000, 
                        InvoiceTypeId = 2, 
                        TaxRateId = 2, 
                        TaxAmount = 3600, // 6%
                        AmountWithoutTax = 56400,
                        Status = "已开票", 
                        InvoiceType_ = "采购"
                    }
                };
                context.Invoices.AddRange(invoices);
                context.SaveChanges();
                
                // 添加交易记录
                var transactions = new List<Transaction>
                {
                    // 收入交易
                    new Transaction { 
                        ProjectId = 2, 
                        AccountId = 2, 
                        Amount = 60000, 
                        TransactionType = "收入", 
                        IncomeCategoryId = 2, 
                        Description = "软件开发首付款", 
                        TransactionDate = now.AddMonths(-2), 
                        CreatedById = 1, 
                        CreatedAt = now.AddMonths(-2) 
                    },
                    new Transaction { 
                        ProjectId = 1, 
                        AccountId = 2, 
                        Amount = 30000, 
                        TransactionType = "收入", 
                        IncomeCategoryId = 2, 
                        Description = "办公室装修首付款", 
                        TransactionDate = now.AddMonths(-1), 
                        CreatedById = 1, 
                        CreatedAt = now.AddMonths(-1) 
                    },
                    
                    // 支出交易
                    new Transaction { 
                        ProjectId = 1, 
                        AccountId = 2, 
                        Amount = 20000, 
                        TransactionType = "支出", 
                        ExpenseCategoryId = 1, 
                        Description = "购买办公用品", 
                        TransactionDate = now.AddDays(-15), 
                        CreatedById = 1, 
                        CreatedAt = now.AddDays(-15) 
                    },
                    new Transaction { 
                        ProjectId = 3, 
                        AccountId = 2, 
                        Amount = 5000, 
                        TransactionType = "支出", 
                        ExpenseCategoryId = 5, 
                        Description = "市场调研差旅费", 
                        TransactionDate = now.AddDays(-10), 
                        CreatedById = 1, 
                        CreatedAt = now.AddDays(-10) 
                    },
                    
                    // 工资支出
                    new Transaction { 
                        ProjectId = 2, 
                        AccountId = 2, 
                        Amount = 51800, // 所有员工薪资总和
                        TransactionType = "支出", 
                        ExpenseCategoryId = 4, 
                        Description = lastMonth.ToString("yyyy年MM月") + "员工工资", 
                        TransactionDate = lastMonth.AddDays(25), 
                        CreatedById = 1, 
                        CreatedAt = lastMonth.AddDays(25) 
                    },
                    
                    // 转账交易 - 从银行卡到现金
                    new Transaction { 
                        ProjectId = 1, 
                        AccountId = 2, 
                        Amount = 10000, 
                        TransactionType = "支出", 
                        Description = "转账到现金账户", 
                        TransactionDate = now.AddDays(-5), 
                        CreatedById = 1, 
                        CreatedAt = now.AddDays(-5) 
                    },
                    new Transaction { 
                        ProjectId = 1, 
                        AccountId = 1, 
                        Amount = 10000, 
                        TransactionType = "收入", 
                        Description = "从银行卡转入", 
                        TransactionDate = now.AddDays(-5), 
                        CreatedById = 1, 
                        CreatedAt = now.AddDays(-5) 
                    }
                };
                context.Transactions.AddRange(transactions);
                context.SaveChanges();
            }
        }

        // 密码哈希辅助方法
        private static string HashPassword(string password)
        {
            // 简单的密码哈希示例，生产环境建议使用更安全的方法
            return Convert.ToBase64String(
                System.Security.Cryptography.SHA256.Create().ComputeHash(
                    System.Text.Encoding.UTF8.GetBytes(password)
                )
            );
        }
    }
} 