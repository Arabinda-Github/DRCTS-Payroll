using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Model
{
    public class ShiftDetails
    {
        public int AssignmentID { get; set; }
        public int EmployeeID { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? ShiftCode { get; set; }
        public string? ShiftName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int OfficeID { get; set; }
        public string? OfficeName { get; set; }
        public string? OfficeAddress { get; set; }
        public decimal OfficeLatitude { get; set; }
        public decimal OfficeLongitude { get; set; }
        public int GeoFenceRadius { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }
}
