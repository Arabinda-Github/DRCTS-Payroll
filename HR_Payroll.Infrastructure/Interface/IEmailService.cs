using HR_Payroll.Core.Model.Email;
using System.Net.Mail;

namespace HR_Payroll.Infrastructure.Interface
{
    public interface IEmailService
    {
        Task<EmailResult> SendEmailAsync(string to, string subject, string body, EmailConfiguration config, bool isHtml = true);
        Task<EmailResult> SendEmailAsync(List<string> recipients, string subject, string body, EmailConfiguration config,
                                         bool isHtml = true, List<string>? cc = null, List<string>? bcc = null,
                                         List<Attachment>? attachments = null);
        Task<EmailResult> SendPasswordResetEmailAsync(string to, string userName, string resetLink, EmailConfiguration config);
        Task<BulkEmailResult> SendBulkEmailsAsync(List<EmailRequest> emailRequests, EmailConfiguration config);
    }
}
