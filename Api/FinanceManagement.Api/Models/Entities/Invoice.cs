using System;

namespace FinanceManagement.Api.Models.Entities
{
    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public int ContractId { get; set; }
        public virtual Contract Contract { get; set; }
        public int InvoiceTypeId { get; set; }
        public virtual InvoiceType InvoiceType { get; set; }
        public decimal Amount { get; set; }
        public string InvoiceType_ { get; set; } // 销售/采购
        public DateTime InvoiceDate { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? TaxRateId { get; set; }
        public virtual TaxRate TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal AmountWithoutTax { get; set; }
    }
} 