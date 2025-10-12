using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HR_Payroll.CommonCases.Utility
{
    public class ExternalHelper
    {
        private static readonly Random _random = new Random();

        #region------------ Random Number Generation -------------

        private int GenerateUniqueNumber()
        {
            // Generate a random number between 100000 and 999999 (6 digits)
            int randomNumber = _random.Next(100000, 1000000);
            return randomNumber;
        }

        public static string GenerateRandomNumber(int length)
        {
            const string chars = "0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        public async Task<string> GenerateBookingReferenceNumberAsync()
        {
            try
            {
                string currentDate = DateTime.Now.ToString("yyyyMMdd");
                string uniqueNo = GenerateRandomNumber(6);

                // Get the count of bookings for today
                int bookingCount = GenerateUniqueNumber();//await Db.Booking.CountAsync();

                // Increment by 1 to get the next booking number
                int nextBookingNumber = bookingCount + 1;

                // Format the booking reference number
                string bookingRefNo = $"HRM{uniqueNo}{currentDate}{nextBookingNumber:D4}";

                return bookingRefNo;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        //6-Digit OTP generation
        public static int Create6digitOtp()
        {
            // Use cryptographically secure random number generation for OTP
            int min = 100000;
            int max = 999999;
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[4];
                int otp;
                do
                {
                    rng.GetBytes(bytes);
                    otp = BitConverter.ToInt32(bytes, 0);
                    otp = Math.Abs(otp % (max - min + 1)) + min;
                } while (otp < min || otp > max);
                return otp;
            }
        }

        //4-Digit OTP generation
        public static int Create4digitOtp()
        {
            // Use cryptographically secure random number generation for OTP
            int min = 1000;
            int max = 9999;
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[4];
                int otp;
                do
                {
                    rng.GetBytes(bytes);
                    otp = BitConverter.ToInt32(bytes, 0);
                    otp = Math.Abs(otp % (max - min + 1)) + min;
                } while (otp < min || otp > max);
                return otp;
            }
        }

        #endregion

        #region------------ DateTime Conversion -------------

        public static DateTime ConvertToDateTime(string date)
        {
            if (string.IsNullOrEmpty(date)) throw new ArgumentNullException(nameof(date));
            var dateParts = date.Split('-');
            if (dateParts.Length != 3) throw new FormatException("Invalid date format. Expected format: dd-MM-yyyy");
            int day = int.Parse(dateParts[0]);
            int month = int.Parse(dateParts[1]);
            int year = int.Parse(dateParts[2]);
            return new DateTime(year, month, day);
        }
        #endregion

        #region------------File Upload Extention -------------

        public static string ReturnFileSize(string path)
        {
            string filepath = Directory.GetCurrentDirectory() + "/wwwroot" + path;
            FileInfo fi = new FileInfo(filepath);
            if (fi.Exists)
            {
                decimal fileSize = fi.Length / 1024;
                if (fileSize > 1024)
                {
                    fileSize = fileSize / 1024;
                    fileSize = Math.Round(fileSize, 2);
                    return fileSize.ToString() + " mb";
                }
                else
                {
                    fileSize = Math.Round(fileSize, 2);
                    return fileSize.ToString() + " kb";
                }
            }
            else { return ""; }
        }

        public static string ReturnFileType(string path)
        {
            string filepath = Directory.GetCurrentDirectory() + "/wwwroot" + path;
            FileInfo fi = new FileInfo(filepath);
            if (fi.Exists)
            {
                var filename = fi.Extension;
                return filename.Replace(".", "");
            }
            else { return ""; }
        }

        public static string GenerateFileName(string fileextenstion)
        {
            if (fileextenstion == null) throw new ArgumentNullException(nameof(fileextenstion));
            return $"{Guid.NewGuid():N}_{DateTime.Now:yyyyMMddHHmmssfff}{fileextenstion}";
        }

        public static string FileUpload(IFormFile pdf_attachment, string fileextenstion, string folderName)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", folderName);
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            var filepath = new PhysicalFileProvider(path).Root;
            var attachileName = GenerateFileName(fileextenstion);
            var filepath_Attach = filepath + $@"\{attachileName}";
            var stream = new FileStream(filepath_Attach, FileMode.Create);
            pdf_attachment.CopyTo(stream);
            return "/Upload/" + folderName + "/" + attachileName;
        }

        public static string FileUploadThroughApi(IFormFile file, string baseDirectory, string folderName)
        {           
            string path = Path.Combine(baseDirectory, "wwwroot", "Upload", folderName);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            var fileName = GenerateFileName(Path.GetExtension(file.FileName));
            var filePath = Path.Combine(path, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            return $"/Upload/{folderName}/{fileName}";
        }

        #endregion

        #region------------Encrypt/ Decrypt Extention -------------
        // Replace DESCryptoServiceProvider with DESCryptoService.Create() in Encrypt and Decrypt methods

        public static string? Encrypt(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            string EncrptKey = "[@DRCTS_Pvt.Ltd.2024]";
            byte[] byKey = { };
            byte[] IV = { 18, 52, 86, 120, 144, 171, 205, 239 };
            byKey = Encoding.UTF8.GetBytes(EncrptKey.Substring(0, 8));
            using (var des = DES.Create())
            {
                byte[] inputByteArray = Encoding.UTF8.GetBytes(str);
                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(byKey, IV), CryptoStreamMode.Write))
                {
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string? Decrypt(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            str = str.Replace(" ", "+");
            string DecryptKey = "[@DRCTS_Pvt.Ltd.2024]";
            byte[] byKey = { };
            byte[] IV = { 18, 52, 86, 120, 144, 171, 205, 239 };
            byKey = Encoding.UTF8.GetBytes(DecryptKey.Substring(0, 8));
            using (var des = DES.Create())
            {
                byte[] inputByteArray = Convert.FromBase64String(str.Replace(" ", "+"));
                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(byKey, IV), CryptoStreamMode.Write))
                {
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                    Encoding encoding = Encoding.UTF8;
                    return encoding.GetString(ms.ToArray());
                }
            }
        }

        #endregion
    }
}
