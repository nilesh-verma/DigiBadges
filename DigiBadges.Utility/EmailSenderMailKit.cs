using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace DigiBadges.Utility
{
  public  class EmailSenderMailKit
    {
        public static void Email(string email, string body,string subject)
        {
            try
            {
                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                message.From = new MailAddress("thisistestuser1244@gmail.com");
                message.To.Add(new MailAddress(email));
                message.Subject = subject;
                message.IsBodyHtml = true; //to make message body as html  
                message.Body = body;

                smtp.Port = 587;
                smtp.Host = "smtp.gmail.com"; //for gmail host  
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential("thisistestuser1244@gmail.com", " Jeet@123");
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Send(message);
            }
            catch (Exception e)
            {

                Console.WriteLine(e);

            }
            /*        public Task SendEmailAsync(string email, string subject, string htmlMessage)
                {
                    return Execute(emailOptions.SendGridKey, subject, htmlMessage, email);
                }
                private Task Execute(string sendGridKEy, string subject, string message, string email)
                {
                    var client = new SendGridClient(sendGridKEy);
                    var from = new EmailAddress("admin@digibadge.com", "DigiBadge");
                    var to = new EmailAddress(email, "End User");
                    var msg = MailHelper.CreateSingleEmail(from, to, subject, "", message);
                    return client.SendEmailAsync(msg);
                }*/

        }
    }
}
