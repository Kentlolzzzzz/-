using System;

namespace FinanceManagement.Api.Models.Entities
{
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string EmployeeNumber { get; set; }
        public string Position { get; set; }
        public string Department { get; set; }
        public DateTime HireDate { get; set; }
        public decimal BaseSalary { get; set; }
        public string Status { get; set; }
        public string ContactInfo { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
} 