using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.DTO
{
    public class AttendanceDto
    {
        public DateTime AttendanceDate { get; set; }
        public string? DayName { get; set; }
        public string? IsWeekend { get; set; }
        public string? ShiftName { get; set; }
        public string? CheckInTime { get; set; }
        public string? CheckOutTime { get; set; }
        public string? WorkingHoursFormatted { get; set; }
        public string? Status { get; set; }
    }
}
