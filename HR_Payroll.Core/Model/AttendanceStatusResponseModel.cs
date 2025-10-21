using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Model
{
    public class AttendanceStatusResponseModel
    {
        // Attendance Details
        public int? AttendanceID { get; set; }
        public int EmployeeID { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public DateTime AttendanceDate { get; set; }
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }

        // Location Details
        public decimal? CheckInLatitude { get; set; }
        public decimal? CheckInLongitude { get; set; }
        public string? CheckInLocation { get; set; }
        public string? CheckInAddress { get; set; }
        public decimal? CheckOutLatitude { get; set; }
        public decimal? CheckOutLongitude { get; set; }
        public string? CheckOutLocation { get; set; }
        public string? CheckOutAddress { get; set; }

        // Geofence Details
        public bool? IsCheckInWithinGeofence { get; set; }
        public decimal? CheckInDistance { get; set; }
        public bool? IsCheckOutWithinGeofence { get; set; }
        public decimal? CheckOutDistance { get; set; }

        // Status Details
        public string? Status { get; set; }
        public string? AttendanceType { get; set; }
        public string? Remarks { get; set; }

        // Calculated Fields
        public decimal WorkingHours { get; set; }
        public int ElapsedSeconds { get; set; }

        // Status Flags
        public bool IsCheckedIn { get; set; }
        public bool IsCheckedOut { get; set; }
        public string? CurrentStatus { get; set; }

        // Shift Details
        public string? ShiftCode { get; set; }
        public string? ShiftName { get; set; }
        public TimeSpan? ShiftStartTime { get; set; }
        public TimeSpan? ShiftEndTime { get; set; }
        public int? GracePeriodMinutes { get; set; }
        public int? HalfDayMinutes { get; set; }
        public int? FullDayMinutes { get; set; }

        // Office Details
        public int? OfficeID { get; set; }
        public string? OfficeName { get; set; }
        public string? OfficeCode { get; set; }
        public decimal? OfficeLatitude { get; set; }
        public decimal? OfficeLongitude { get; set; }
        public int? GeoFenceRadius { get; set; }
        public string? OfficeAddress { get; set; }

        // Employee Details
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
    }
}
