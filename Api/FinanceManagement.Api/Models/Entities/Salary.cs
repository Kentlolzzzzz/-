using System;

namespace FinanceManagement.Api.Models.Entities
{
    public class Salary
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }
        public DateTime SalaryMonth { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal Bonus { get; set; }
        public decimal Deduction { get; set; }
        public decimal ActualPayment { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime? PaymentDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
} 