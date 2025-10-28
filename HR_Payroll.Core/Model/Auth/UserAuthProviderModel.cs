using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Core.Model.Auth
{
    public class UserAuthProviderModel
    {
        public int ProviderID { get; set; }              // Primary Key
        public int UserID { get; set; }                  // FK to Users table
        public string? ProviderName { get; set; }         // e.g., Google, Facebook, System
        public string? ProviderUserID { get; set; }       // ID of the user in the provider system
        public string? AccessToken { get; set; }          // Optional
        public string? RefreshToken { get; set; }         // Refresh token
        public DateTime? TokenExpiry { get; set; }       // Expiration date/time
        public DateTime? LinkedDate { get; set; }        // When linked
        public char? Del_Flg { get; set; }               // Y/N
        public DateTime? CreatedDate { get; set; }       // Record created
        public string? CreatedBy { get; set; }            // Created by
        public DateTime? ModifiedDate { get; set; }      // Last modified
        public string? ModifiedBy { get; set; }           // Modified by
    }
}
