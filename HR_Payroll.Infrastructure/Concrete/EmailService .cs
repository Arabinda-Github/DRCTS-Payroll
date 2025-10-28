using HR_Payroll.CommonCases.Email;
using HR_Payroll.Core.Model.Email;
using HR_Payroll.Infrastructure.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace HR_Payroll.Infrastructure.Concrete
{
    public class EmailService : IEmailService, IDisposable
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public async Task<EmailResult> SendEmailAsync(string to, string subject, string body, EmailConfiguration config, bool isHtml = true)
        {
            return await SendEmailAsync(new List<string> { to }, subject, body, config, isHtml);
        }

        public async Task<EmailResult> SendEmailAsync(List<string> recipients, string subject, string body,EmailConfiguration config, bool isHtml = true,
            List<string>? cc = null, List<string>? bcc = null,List<Attachment>? attachments = null)
        {
            if (recipients == null || recipients.Count == 0)
                return EmailResult.Failure("No recipients specified.");
            if (string.IsNullOrWhiteSpace(subject))
                return EmailResult.Failure("Subject is required.");
            if (string.IsNullOrWhiteSpace(body))
                return EmailResult.Failure("Body is required.");

            var start = DateTime.UtcNow;

            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(config.FromEmail!, config.FromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml,
                    Priority = MailPriority.Normal
                };

                // ✅ Add Recipients
                foreach (var recipient in recipients.Where(IsValidEmail))
                    message.To.Add(recipient);

                if (message.To.Count == 0)
                    return EmailResult.Failure("No valid recipients found.");

                // ✅ Add CC & BCC
                cc?.Where(IsValidEmail).ToList().ForEach(ccEmail => message.CC.Add(ccEmail));
                bcc?.Where(IsValidEmail).ToList().ForEach(bccEmail => message.Bcc.Add(bccEmail));

                // ✅ Add Attachments
                attachments?.ForEach(a => message.Attachments.Add(a));

                var smtpClient = EmailSms_Sender.GetSmtpClient(config);
                await smtpClient.SendMailAsync(message);

                var duration = DateTime.UtcNow - start;
                _logger.LogInformation("✅ Email sent to {Count} recipients in {Ms}ms | Subject: {Subject}",
                    message.To.Count, duration.TotalMilliseconds, subject);

                return EmailResult.Success($"Email sent successfully in {duration.TotalMilliseconds:F2} ms.");
            }
            catch (SmtpException ex)
            {
                EmailSms_Sender.ResetSmtpClient();
                _logger.LogError(ex, "SMTP error while sending email: {Message}", ex.Message);
                return EmailResult.Failure($"SMTP error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email: {Message}", ex.Message);
                return EmailResult.Failure($"Error: {ex.Message}");
            }
        }

        public async Task<EmailResult> SendPasswordResetEmailAsync(string to, string userName, string resetLink, EmailConfiguration config)
        {
            var subject = "Password Reset Request";
            var body = GetPasswordResetTemplate(userName, resetLink);
            return await SendEmailAsync(to, subject, body, config, isHtml: true);
        }

        public async Task<BulkEmailResult> SendBulkEmailsAsync(List<EmailRequest> emailRequests, EmailConfiguration config)
        {
            var results = new BulkEmailResult();
            var tasks = emailRequests.Select(req =>
                SendEmailAsync(req.Recipients, req.Subject, req.Body, config, req.IsHtml)).ToList();

            var emailResults = await Task.WhenAll(tasks);

            results.TotalSent = emailResults.Count(r => r.success);
            results.TotalFailed = emailResults.Length - results.TotalSent;
            results.Results = emailResults.ToList();

            return results;
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                return new MailAddress(email).Address == email;
            }
            catch
            {
                return false;
            }
        }

        private string GetPasswordResetTemplate(string userName, string resetLink) =>
            $@"
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset='utf-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                           color: white; padding: 25px; text-align: center; border-radius: 10px 10px 0 0; }}
                .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                .button {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                           color: primary; padding: 12px 25px; border-radius: 5px; text-decoration: none; display: inline-block; }}
                .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
            </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'><h2>Password Reset Request</h2></div>
                    <div class='content'>
                        <p>Hello <b>{userName}</b>,</p>
                        <p>Click below to reset your password. This link expires in <b>30 minutes</b>:</p>
                        <p style='text-align:center;'><a href='{resetLink}' class='button'>Reset Password</a></p>
                        <p>If you did not request this, please ignore this email.</p>
                    </div>
                    <div class='footer'>
                        &copy; {DateTime.UtcNow.Year} HR Payroll. All rights reserved.
                    </div>
                </div>
            </body>
            </html>";

        public void Dispose() => EmailSms_Sender.ResetSmtpClient();
    }
}
