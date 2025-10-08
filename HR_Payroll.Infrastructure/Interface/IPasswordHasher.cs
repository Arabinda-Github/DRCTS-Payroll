using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.Infrastructure.Interface
{
    public interface IPasswordHasher
    {
        bool VerifyBase64Password(string password, string Base64Password);
    }
}
