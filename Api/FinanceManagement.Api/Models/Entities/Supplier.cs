using System;

namespace FinanceManagement.Api.Models.Entities
{
    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ContactPerson { get; set; }
        public string ContactInfo { get; set; }
        public string Address { get; set; }
        public string TaxId { get; set; }
        public string BankAccount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
} 