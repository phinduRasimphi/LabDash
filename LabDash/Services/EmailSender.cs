
using LabDash.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace LabDash.Services
{
    public interface IEmailAttachmentSender
    {
        Task SendEmailWithAttachmentAsync(string email, string subject, string htmlMessage, byte[] attachment, string attachmentName);
    }
    public class EmailSender : IEmailSender, IEmailAttachmentSender
    {
        private readonly EmailSettings _settings;

        public EmailSender(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            using var message = new MailMessage();
            message.From = new MailAddress(_settings.FromAddress);
            message.Subject = subject;
            message.To.Add(new MailAddress(email));
            message.Body = htmlMessage;
            message.IsBodyHtml = true;

            using var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(_settings.FromAddress, _settings.AppPassword),
                EnableSsl = true,
            };

            await smtpClient.SendMailAsync(message);
        }

        public async Task SendEmailWithAttachmentAsync(string email, string subject, string htmlMessage, byte[] attachment, string attachmentName)
        {
            using var message = new MailMessage();
            message.From = new MailAddress(_settings.FromAddress);
            message.Subject = subject;
            message.To.Add(new MailAddress(email));
            message.Body = htmlMessage;
            message.IsBodyHtml = true;

            using var stream = new MemoryStream(attachment);
            message.Attachments.Add(new Attachment(stream, attachmentName));

            using var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(_settings.FromAddress, _settings.AppPassword),
                EnableSsl = true,
            };

            await smtpClient.SendMailAsync(message);
        }
    }
}