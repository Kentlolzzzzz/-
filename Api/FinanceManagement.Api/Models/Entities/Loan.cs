using System;

namespace FinanceManagement.Api.Models.Entities
{
    public class Loan
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }
        public decimal LoanAmount { get; set; }
        public DateTime LoanDate { get; set; }
        public string LoanPurpose { get; set; }
        public int RepaymentMonths { get; set; }
        public decimal MonthlyPayment { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
} 