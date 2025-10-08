using HR_Payroll.Infrastructure.Interface;
using HR_Payroll.CommonCases.Utility;

namespace HR_Payroll.Infrastructure.Concrete
{
    public class PasswordHasher : IPasswordHasher
    {
        public bool VerifyBase64Password(string password, string Base64Password)
        {
            var encryptbase64 = ExternalHelper.Encrypt(password);
            if (encryptbase64 == Base64Password)
            {
                return true;
            }
            return false;
        }
    }
}
