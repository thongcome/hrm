namespace HRM.Services
{
    using Microsoft.AspNetCore.Identity.UI.Services;
    using System.Net;
    using System.Net.Mail;
    using System.Threading.Tasks;

    public class BasicEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public BasicEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            //var smtpSettings = _configuration.GetSection("SmtpSettings");
            //var host = smtpSettings["Host"];
            //var port = int.Parse(smtpSettings["Port"]);
            //var enableSsl = bool.Parse(smtpSettings["EnableSSL"]);
            //var user = smtpSettings["Username"];
            //var password = smtpSettings["Password"];
            //var fromEmail = smtpSettings["SenderEmail"];

            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var host = smtpSettings["Host"] ?? throw new ArgumentNullException("SMTP Host is missing in configuration");
            var port = int.TryParse(smtpSettings["Port"], out var smtpPort) ? smtpPort : throw new ArgumentNullException("SMTP Port is missing in configuration");
            var enableSsl = bool.TryParse(smtpSettings["EnableSSL"], out var sslEnabled) && sslEnabled;
            var sFrom = smtpSettings["SenderEmail"] ?? throw new ArgumentNullException("SenderEmail is missing in configuration");
            var fromAddress = new MailAddress(sFrom) ;
            var fromEmail = smtpSettings["SenderEmail"] ?? throw new ArgumentNullException("Sender is missing in configuration");
        
            var user = smtpSettings["Username"];
            var password = smtpSettings["Password"];

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, password),
                EnableSsl = enableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(email);
          //  mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString("This is the plain text version of the email.", null, "text/plain"));

            await client.SendMailAsync(mailMessage);
        }
    }

}
