using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;


namespace NakhlaBelal.Utitlies
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("bb550334@gmail.com", "vyba lkpi plxl gfoy")
            };

            return client.SendMailAsync(
            new MailMessage(from: "bb550334@gmail.com",
                            to: email,
                            subject,
                            htmlMessage
                            )
            {
                IsBodyHtml = true
            });
        }
    }
}
