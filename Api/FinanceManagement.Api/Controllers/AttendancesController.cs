using FinanceManagement.Api.Data;
using FinanceManagement.Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinanceManagement.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AttendancesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AttendancesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Attendances
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Attendance>>> GetAttendances()
        {
            return await _context.Attendances
                .Include(a => a.Employee)
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();
        }

        // GET: api/Attendances/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Attendance>> GetAttendance(int id)
        {
            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendance == null)
            {
                return NotFound();
            }

            return attendance;
        }

        // GET: api/Attendances/Employee/5
        [HttpGet("Employee/{employeeId}")]
        public async Task<ActionResult<IEnumerable<Attendance>>> GetEmployeeAttendances(int employeeId)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
            {
                return NotFound("指定的员工不存在");
            }

            return await _context.Attendances
                .Where(a => a.EmployeeId == employeeId)
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();
        }

        // GET: api/Attendances/Date/2024-03-15
        [HttpGet("Date/{date}")]
        public async Task<ActionResult<IEnumerable<Attendance>>> GetDateAttendances(string date)
        {
            if (!DateTime.TryParse(date, out DateTime attendanceDate))
            {
                return BadRequest("日期格式不正确，应为 yyyy-MM-dd");
            }

            return await _context.Attendances
                .Include(a => a.Employee)
                .Where(a => a.AttendanceDate.Date == attendanceDate.Date)
                .OrderBy(a => a.Employee.Name)
                .ToListAsync();
        }

        // GET: api/Attendances/Sheet
        [HttpGet("Sheet")]
        public async Task<ActionResult<object>> GetAttendanceSheet([FromQuery] int year, [FromQuery] int month)
        {
            if (year <= 0 || month <= 0 || month > 12)
            {
                return BadRequest("无效的年份或月份");
            }

            // 获取所有在职员工
            var employees = await _context.Employees
                .Where(e => e.Status == "Active")
                .OrderBy(e => e.Department)
                .ThenBy(e => e.Name)
                .ToListAsync();

            if (!employees.Any())
            {
                return NotFound("没有在职员工");
            }

            // 获取指定月份的所有考勤记录
            var firstDay = new DateTime(year, month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);
            
            var attendances = await _context.Attendances
                .Where(a => a.AttendanceDate >= firstDay && a.AttendanceDate <= lastDay)
                .ToListAsync();

            // 生成每个员工的考勤表
            var result = new List<object>();
            
            foreach (var employee in employees)
            {
                var employeeAttendances = attendances
                    .Where(a => a.EmployeeId == employee.Id)
                    .ToDictionary(a => a.AttendanceDate.Day, a => a);
                
                // 计算出勤统计
                int normalCount = employeeAttendances.Count(a => a.Value.Status == "Normal");
                int lateCount = employeeAttendances.Count(a => a.Value.Status == "Late");
                int absentCount = employeeAttendances.Count(a => a.Value.Status == "Absent");
                int leaveCount = employeeAttendances.Count(a => a.Value.Status == "Leave");
                
                // 生成日期记录
                var dailyRecords = new Dictionary<int, object>();
                for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
                {
                    if (employeeAttendances.TryGetValue(day, out var attendance))
                    {
                        dailyRecords[day] = new 
                        {
                            Status = attendance.Status,
                            CheckIn = attendance.CheckIn?.ToString(@"hh\:mm"),
                            CheckOut = attendance.CheckOut?.ToString(@"hh\:mm"),
                            WorkHours = attendance.WorkHours,
                            Remarks = attendance.Remarks
                        };
                    }
                    else
                    {
                        // 检查是否是周末
                        var date = new DateTime(year, month, day);
                        bool isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
                        
                        dailyRecords[day] = new 
                        {
                            Status = isWeekend ? "Weekend" : "Unknown",
                            CheckIn = (string)null,
                            CheckOut = (string)null,
                            WorkHours = 0m,
                            Remarks = isWeekend ? "周末" : ""
                        };
                    }
                }
                
                result.Add(new 
                {
                    Employee = new 
                    {
                        employee.Id,
                        employee.Name,
                        employee.EmployeeNumber,
                        employee.Department,
                        employee.Position
                    },
                    Summary = new 
                    {
                        NormalCount = normalCount,
                        LateCount = lateCount,
                        AbsentCount = absentCount,
                        LeaveCount = leaveCount,
                        TotalWorkHours = employeeAttendances.Sum(a => a.Value.WorkHours)
                    },
                    DailyRecords = dailyRecords
                });
            }
            
            return new
            {
                Year = year,
                Month = month,
                TotalDays = DateTime.DaysInMonth(year, month),
                EmployeeCount = employees.Count,
                AttendanceRecords = result
            };
        }

        // POST: api/Attendances
        [HttpPost]
        public async Task<ActionResult<Attendance>> CreateAttendance(Attendance attendance)
        {
            // 验证员工是否存在
            var employee = await _context.Employees.FindAsync(attendance.EmployeeId);
            if (employee == null)
            {
                return BadRequest("指定的员工不存在");
            }

            // 验证同一天不能重复添加同一员工的考勤记录
            bool duplicateAttendance = await _context.Attendances.AnyAsync(a => 
                a.EmployeeId == attendance.EmployeeId && 
                a.AttendanceDate.Date == attendance.AttendanceDate.Date);
                
            if (duplicateAttendance)
            {
                return BadRequest("该员工在此日期已有考勤记录");
            }

            // 计算工作时长（如果提供了签到和签退时间）
            if (attendance.CheckIn.HasValue && attendance.CheckOut.HasValue)
            {
                // 将签到签退时间与考勤日期合并
                var checkInDateTime = attendance.AttendanceDate.Date.Add(attendance.CheckIn.Value);
                var checkOutDateTime = attendance.AttendanceDate.Date.Add(attendance.CheckOut.Value);
                
                // 如果签退时间小于签到时间，假设是跨天的情况，加一天
                if (checkOutDateTime < checkInDateTime)
                {
                    checkOutDateTime = checkOutDateTime.AddDays(1);
                }
                
                attendance.WorkHours = (decimal)(checkOutDateTime - checkInDateTime).TotalHours;
                
                // 根据签到时间判断是否迟到
                if (attendance.Status == "Normal" && attendance.CheckIn > new TimeSpan(9, 0, 0))
                {
                    attendance.Status = "Late";
                    attendance.Remarks = string.IsNullOrEmpty(attendance.Remarks)
                        ? "迟到"
                        : $"迟到, {attendance.Remarks}";
                }
            }
            
            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAttendance), new { id = attendance.Id }, attendance);
        }

        // PUT: api/Attendances/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAttendance(int id, Attendance attendance)
        {
            if (id != attendance.Id)
            {
                return BadRequest();
            }

            // 计算工作时长（如果提供了签到和签退时间）
            if (attendance.CheckIn.HasValue && attendance.CheckOut.HasValue)
            {
                // 将签到签退时间与考勤日期合并
                var checkInDateTime = attendance.AttendanceDate.Date.Add(attendance.CheckIn.Value);
                var checkOutDateTime = attendance.AttendanceDate.Date.Add(attendance.CheckOut.Value);
                
                // 如果签退时间小于签到时间，假设是跨天的情况，加一天
                if (checkOutDateTime < checkInDateTime)
                {
                    checkOutDateTime = checkOutDateTime.AddDays(1);
                }
                
                attendance.WorkHours = (decimal)(checkOutDateTime - checkInDateTime).TotalHours;
                
                // 根据签到时间判断是否迟到
                if (attendance.Status == "Normal" && attendance.CheckIn > new TimeSpan(9, 0, 0))
                {
                    attendance.Status = "Late";
                    attendance.Remarks = string.IsNullOrEmpty(attendance.Remarks)
                        ? "迟到"
                        : $"迟到, {attendance.Remarks}";
                }
            }

            _context.Entry(attendance).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await AttendanceExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Attendances/BatchCreate
        [HttpPost("BatchCreate")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Attendance>>> BatchCreateAttendances(List<Attendance> attendances)
        {
            if (attendances == null || !attendances.Any())
            {
                return BadRequest("请提供有效的考勤记录列表");
            }

            // 验证员工是否存在并检查重复记录
            foreach (var attendance in attendances)
            {
                // 验证员工是否存在
                var employee = await _context.Employees.FindAsync(attendance.EmployeeId);
                if (employee == null)
                {
                    return BadRequest($"员工ID {attendance.EmployeeId} 不存在");
                }

                // 验证同一天不能重复添加同一员工的考勤记录
                bool duplicateAttendance = await _context.Attendances.AnyAsync(a => 
                    a.EmployeeId == attendance.EmployeeId && 
                    a.AttendanceDate.Date == attendance.AttendanceDate.Date);
                    
                if (duplicateAttendance)
                {
                    return BadRequest($"员工 {employee.Name} 在 {attendance.AttendanceDate.ToString("yyyy-MM-dd")} 已有考勤记录");
                }

                // 计算工作时长（如果提供了签到和签退时间）
                if (attendance.CheckIn.HasValue && attendance.CheckOut.HasValue)
                {
                    // 将签到签退时间与考勤日期合并
                    var checkInDateTime = attendance.AttendanceDate.Date.Add(attendance.CheckIn.Value);
                    var checkOutDateTime = attendance.AttendanceDate.Date.Add(attendance.CheckOut.Value);
                    
                    // 如果签退时间小于签到时间，假设是跨天的情况，加一天
                    if (checkOutDateTime < checkInDateTime)
                    {
                        checkOutDateTime = checkOutDateTime.AddDays(1);
                    }
                    
                    attendance.WorkHours = (decimal)(checkOutDateTime - checkInDateTime).TotalHours;
                    
                    // 根据签到时间判断是否迟到
                    if (attendance.Status == "Normal" && attendance.CheckIn > new TimeSpan(9, 0, 0))
                    {
                        attendance.Status = "Late";
                        attendance.Remarks = string.IsNullOrEmpty(attendance.Remarks)
                            ? "迟到"
                            : $"迟到, {attendance.Remarks}";
                    }
                }
            }

            _context.Attendances.AddRange(attendances);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAttendances), attendances);
        }

        // DELETE: api/Attendances/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAttendance(int id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null)
            {
                return NotFound();
            }

            _context.Attendances.Remove(attendance);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> AttendanceExists(int id)
        {
            return await _context.Attendances.AnyAsync(e => e.Id == id);
        }
    }
} 