using System;

namespace FinanceManagement.Api.Models.Entities
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal Budget { get; set; }
        public string Status { get; set; }
        public int ManagerId { get; set; }
        public virtual User Manager { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
} 