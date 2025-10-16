using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Model
{
    public class CheckOutRequestModel
    {
        public int EmployeeID { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string? Location { get; set; }
        public string? Address { get; set; }
        public string? IPAddress { get; set; }
        public string? DeviceInfo { get; set; }
        public string? Remarks { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
