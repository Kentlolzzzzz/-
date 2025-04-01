using System;

namespace FinanceManagement.Api.Models.Entities
{
    public class Transaction
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public virtual Project Project { get; set; }
        public int AccountId { get; set; }
        public virtual Account Account { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } // 收入/支出
        public int? IncomeCategoryId { get; set; }
        public virtual IncomeCategory IncomeCategory { get; set; }
        public int? ExpenseCategoryId { get; set; }
        public virtual ExpenseCategory ExpenseCategory { get; set; }
        public string Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public int CreatedById { get; set; }
        public virtual User CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string AttachmentPath { get; set; }
        public int? TaxRateId { get; set; }
        public virtual TaxRate TaxRate { get; set; }
    }
} 