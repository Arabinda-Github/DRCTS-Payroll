using System.Net;
using System.Net.Mail;
using System.Text;
using HR_Payroll.Core.Model.Email;
using Microsoft.Extensions.Logging;

namespace HR_Payroll.CommonCases.Email
{
    public class EmailSms_Sender
    {
        private WebProxy? objProxy1 = null;
        private readonly ILogger<EmailSms_Sender> _logger;
        private static readonly object _lock = new object();
        private static SmtpClient _smtpClient;
        private static DateTime _lastClientCreation = DateTime.MinValue;
        private static readonly TimeSpan ClientLifetime = TimeSpan.FromMinutes(10);

        public EmailSms_Sender(ILogger<EmailSms_Sender> logger)
        {
            _logger = logger;
        }

        #region--------------------------Mail Section------------------------- 
        //public static bool SendMail(string to, string subject, string body)
        //{
        //    System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();
        //    mail.To.Add(to);
        //    mail.From = new MailAddress("cnbtcuttack@gmail.com");
        //    mail.Subject = subject;
        //    string Body = body;
        //    mail.Body = Body;
        //    mail.Headers.Add("Importance", "High");
        //    mail.Headers.Add("X-Priority", "1");          // 1 = High, 3 = Normal, 5 = Low
        //    mail.Headers.Add("X-MSMail-Priority", "High");
        //    mail.IsBodyHtml = true;
        //    SmtpClient smtp = new SmtpClient();
        //    smtp.Host = "smtp.gmail.com";
        //    smtp.Port = 587;
        //    smtp.UseDefaultCredentials = false;
        //    smtp.Credentials = new System.Net.NetworkCredential("cnbtcuttack@gmail.com", "odfsbztutvtizxbs"); // Enter seders User name and password       
        //    smtp.EnableSsl = true;
        //    smtp.Send(mail);

        //    return true;
        //}

        //public static bool SendMail(string to, string subject, string body)
        //{
        //    System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();
        //    mail.To.Add(to);
        //    mail.From = new MailAddress("transactiondomain@gmail.com");
        //    mail.Subject = subject;
        //    string Body = body;
        //    mail.Body = Body;
        //    mail.IsBodyHtml = true;
        //    SmtpClient smtp = new SmtpClient();
        //    smtp.Host = "smtp.gmail.com";
        //    smtp.Port = 587;
        //    smtp.UseDefaultCredentials = false;
        //    smtp.Credentials = new System.Net.NetworkCredential("transactiondomain@gmail.com", "zaienkklwnmholof"); // Enter seders User name and password       
        //    smtp.EnableSsl = true;
        //    smtp.Send(mail);

        //    return true;
        //}

        public static SmtpClient GetSmtpClient(EmailConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            lock (_lock)
            {
                if (_smtpClient == null || DateTime.UtcNow - _lastClientCreation > ClientLifetime)
                {
                    _smtpClient?.Dispose();

                    _smtpClient = new SmtpClient
                    {
                        Host = config.SmtpHost,
                        Port = config.SmtpPort,
                        EnableSsl = config.EnableSsl,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(config.Username, config.Password),
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        Timeout = config.Timeout * 1000 // Convert seconds → ms
                    };

                    _lastClientCreation = DateTime.UtcNow;
                }

                return _smtpClient;
            }
        }

        public static void ResetSmtpClient()
        {
            lock (_lock)
            {
                _smtpClient?.Dispose();
                _smtpClient = null;
                _lastClientCreation = DateTime.MinValue;
            }
        }

        #endregion

        #region------------------SMS Section------------------------

        private static readonly HttpClient httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        private const string BASE_URL = "http://sms.webrosetechnology.com/httpapi/httpapi";
        private const string TOKEN = "0a6f14aed98c1dfb872c3a433de41918";
        private const string SENDER = "OTCDAC";
        private const string ROUTE = "4";
        private const string TYPE = "1";
        public static async Task<bool> SendSms(string mobile, string sms, string templateId)
        {
            try
            {
                var queryBuilder = new StringBuilder()
                    .Append($"?token={Uri.EscapeDataString(TOKEN)}")
                    .Append($"&sender={Uri.EscapeDataString(SENDER)}")
                    .Append($"&number={Uri.EscapeDataString(mobile)}")
                    .Append($"&route={Uri.EscapeDataString(ROUTE)}")
                    .Append($"&type={Uri.EscapeDataString(TYPE)}")
                    .Append($"&sms={Uri.EscapeDataString(sms)}")
                    .Append($"&templateid={Uri.EscapeDataString(templateId)}");

                var requestUrl = new UriBuilder(BASE_URL)
                {
                    Query = queryBuilder.ToString()
                }.ToString();

                using var response = await httpClient.GetAsync(requestUrl).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                // Log the status code and response content for debugging purposes
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Console.WriteLine($"Failed to send SMS. Status Code: {response.StatusCode}, Response: {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                // Add proper exception handling and logging
                Console.WriteLine($"Exception occurred while sending SMS: {ex}");
                return false;
            }
        }
        public string SendSMS1(string User, string password, string Mobile_Number, string Message)
        {
            string? stringpost = null;
            stringpost = "User=" + User + "&passwd=" + password + "&mobilenumber=" + Mobile_Number + "&message=" + Message;

            HttpWebRequest? objWebRequest = null;
            HttpWebResponse? objWebResponse = null;
            StreamWriter? objStreamWriter = null;
            StreamReader? objStreamReader = null;

            try
            {
                string? stringResult = null;
                objWebRequest = (HttpWebRequest)WebRequest.Create("http://www.smscountry.com/SMSCwebservice_bulk.aspx");
                objWebRequest.Method = "POST";
                if ((objProxy1 != null))
                {
                    objWebRequest.Proxy = objProxy1;
                }
                // Use below code if you want to SETUP PROXY.
                //Parameters to pass: 1. ProxyAddress 2. Port
                //You can find both the parameters in Connection settings of your internet explorer.

                //WebProxy myProxy = new WebProxy("YOUR PROXY", PROXPORT);
                //myProxy.BypassProxyOnLocal = true;
                //wrGETURL.Proxy = myProxy;

                objWebRequest.ContentType = "application/x-www-form-urlencoded";
                objStreamWriter = new StreamWriter(objWebRequest.GetRequestStream());
                objStreamWriter.Write(stringpost);
                objStreamWriter.Flush();
                objStreamWriter.Close();
                objWebResponse = (HttpWebResponse)objWebRequest.GetResponse();
                objStreamReader = new StreamReader(objWebResponse.GetResponseStream());
                stringResult = objStreamReader.ReadToEnd();
                objStreamReader.Close();
                return stringResult;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                if ((objStreamWriter != null))
                {
                    objStreamWriter.Close();
                }
                if ((objStreamReader != null))
                {
                    objStreamReader.Close();
                }
                objWebRequest = null;
                objWebResponse = null;
                objProxy1 = null;
            }
        }

        #endregion

    }
}
