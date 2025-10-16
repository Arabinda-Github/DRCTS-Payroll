using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Model
{
    public class AttendanceStatusResponseModel
    {
        public int? AttendanceID { get; set; }
        public int EmployeeID { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public DateTime AttendanceDate { get; set; }
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public decimal? WorkingHours { get; set; }
        public string? Status { get; set; }
        public string? AttendanceType { get; set; }
        public string? ShiftCode { get; set; }
        public string? ShiftName { get; set; }
        public TimeSpan? ShiftStartTime { get; set; }
        public TimeSpan? ShiftEndTime { get; set; }
        public string? OfficeName { get; set; }
        public int? GeoFenceRadius { get; set; }
        public bool? IsCheckInWithinGeofence { get; set; }
        public decimal? CheckInDistance { get; set; }
        public bool? IsCheckOutWithinGeofence { get; set; }
        public decimal? CheckOutDistance { get; set; }
        public string? CurrentStatus { get; set; } // NOT_CHECKED_IN, CHECKED_IN, CHECKED_OUT
    }
}
