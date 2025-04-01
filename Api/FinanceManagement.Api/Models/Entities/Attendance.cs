using System;

namespace FinanceManagement.Api.Models.Entities
{
    public class Attendance
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string Status { get; set; }
        public TimeSpan? CheckIn { get; set; }
        public TimeSpan? CheckOut { get; set; }
        public decimal? WorkHours { get; set; }
        public string Remarks { get; set; }
    }
} 