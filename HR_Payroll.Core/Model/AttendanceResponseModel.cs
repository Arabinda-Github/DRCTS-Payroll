using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Model
{
    public class AttendanceResponseModel
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public int? AttendanceID { get; set; }
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public bool? IsWithinGeofence { get; set; }
        public decimal? Distance { get; set; }
        public decimal? WorkingHours { get; set; }
    }
}
