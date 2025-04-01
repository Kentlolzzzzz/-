using System;

namespace FinanceManagement.Api.Models.Entities
{
    public class Contract
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; }
        public string ContractType { get; set; } // 销售/采购
        public int ProjectId { get; set; }
        public virtual Project Project { get; set; }
        public int CustomerIdOrSupplierId { get; set; }
        public DateTime SignDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string PaymentTerms { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? TaxRateId { get; set; }
        public virtual TaxRate TaxRate { get; set; }
    }
} 