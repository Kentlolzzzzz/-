using System;

namespace FinanceManagement.Api.Models.Entities
{
    public class TaxRate
    {
        public int Id { get; set; }
        public decimal Rate { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
} 