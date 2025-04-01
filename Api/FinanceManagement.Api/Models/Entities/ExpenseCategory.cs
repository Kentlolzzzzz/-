using System;

namespace FinanceManagement.Api.Models.Entities
{
    public class ExpenseCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public bool IsActive { get; set; } = true;
        public bool ParticipateAccounting { get; set; } = true;
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
} 