using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace DigiBadges.Utility
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailOptions emailOptions;

        public EmailSender(IOptions<EmailOptions> options)
        {
            emailOptions = options.Value;
        }
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            return Execute(email, htmlMessage, subject);
        }
        private Task Execute(string email, string body, string subject)
        {
            MailMessage message = new MailMessage();
            SmtpClient smtp = new SmtpClient();
            message.From = new MailAddress(emailOptions.FromAddress);
            message.To.Add(new MailAddress(email));
            message.Subject = subject;
            message.IsBodyHtml = emailOptions.IsBodyHtml; //to make message body as html  
            message.Body = body;

            smtp.Port = emailOptions.Port;
            smtp.Host = emailOptions.Host; //for gmail host  
            smtp.EnableSsl = emailOptions.EnableSSL;
            smtp.UseDefaultCredentials = emailOptions.UseDefaultCredentials;
            smtp.Credentials = new NetworkCredential(emailOptions.UserName, emailOptions.Password);
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            return smtp.SendMailAsync(message);
        }

    }
}
