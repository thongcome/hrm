using HRM.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net;
using System.Net.Mail;

namespace HRM.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailSender> _logger;


    public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var smtpSettings = _configuration.GetSection("SmtpSettings");
        var host = smtpSettings["Host"] ?? throw new ArgumentNullException("SMTP Host is missing in configuration");
        var port = int.TryParse(smtpSettings["Port"], out var smtpPort) ? smtpPort : throw new ArgumentNullException("SMTP Port is missing in configuration");
        var enableSsl = bool.TryParse(smtpSettings["EnableSSL"], out var sslEnabled) && sslEnabled;
        var sFrom = smtpSettings["SenderEmail"] ?? throw new ArgumentNullException("SenderEmail is missing in configuration");
        var user = smtpSettings["Username"] ?? throw new ArgumentNullException("Username is missing in configuration");
        var password = smtpSettings["Password"] ?? throw new ArgumentNullException("Password is missing in configuration");

        try
        {
            using (var client = new SmtpClient(host, port))
            {
                client.Credentials = new NetworkCredential(user, password);
                client.EnableSsl = enableSsl;
                client.Timeout = 10000;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(sFrom),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                _logger.LogInformation("Attempting to send email to {Recipient}", email);

                // Asynchronous send
                await client.SendMailAsync(mailMessage);

                _logger.LogInformation("Email sent successfully to {Recipient}", email);
            }
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "SMTP error occurred while sending email to {Recipient}: {Message}", email, smtpEx.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "General error occurred while sending email to {Recipient}: {Message}", email, ex.Message);
            throw;
        }
    }

    public async Task SendEmailWithAttachmentAsync(string email, string subject, string htmlMessage,
        string attachmentFileName, byte[] attachmentBytes, string attachmentContentType = "application/pdf")
    {
        var smtpSettings = _configuration.GetSection("SmtpSettings");
        var host = smtpSettings["Host"] ?? throw new ArgumentNullException("SMTP Host is missing in configuration");
        var port = int.TryParse(smtpSettings["Port"], out var smtpPort) ? smtpPort : throw new ArgumentNullException("SMTP Port is missing in configuration");
        var enableSsl = bool.TryParse(smtpSettings["EnableSSL"], out var sslEnabled) && sslEnabled;
        var sFrom = smtpSettings["SenderEmail"] ?? throw new ArgumentNullException("SenderEmail is missing in configuration");
        var user = smtpSettings["Username"] ?? throw new ArgumentNullException("Username is missing in configuration");
        var password = smtpSettings["Password"] ?? throw new ArgumentNullException("Password is missing in configuration");

        try
        {
            using (var client = new SmtpClient(host, port))
            {
                client.Credentials = new NetworkCredential(user, password);
                client.EnableSsl = enableSsl;
                client.Timeout = 20000; // longer than the plain-text path (10000) — attachments take longer

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(sFrom),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                using var attachmentStream = new MemoryStream(attachmentBytes);
                mailMessage.Attachments.Add(new System.Net.Mail.Attachment(attachmentStream, attachmentFileName, attachmentContentType));

                _logger.LogInformation("Attempting to send email with attachment {FileName} to {Recipient}", attachmentFileName, email);

                await client.SendMailAsync(mailMessage);

                _logger.LogInformation("Email with attachment sent successfully to {Recipient}", email);
            }
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "SMTP error occurred while sending email with attachment to {Recipient}: {Message}", email, smtpEx.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "General error occurred while sending email with attachment to {Recipient}: {Message}", email, ex.Message);
            throw;
        }
    }

    //public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    //{
    //    var smtpSettings = _configuration.GetSection("SmtpSettings");
    //    var host = smtpSettings["Host"] ?? throw new ArgumentNullException("SMTP Host is missing in configuration");
    //    var port = int.TryParse(smtpSettings["Port"], out var smtpPort) ? smtpPort : throw new ArgumentNullException("SMTP Port is missing in configuration");
    //    var enableSsl = bool.TryParse(smtpSettings["EnableSSL"], out var sslEnabled) && sslEnabled;
    //    var sFrom = smtpSettings["SenderEmail"] ?? throw new ArgumentNullException("SenderEmail is missing in configuration");
    //    var fromAddress = new MailAddress(sFrom);

    //    try
    //    {
    //        var client = new SmtpClient(host)
    //        {
    //            Port = port,
    //            Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
    //            EnableSsl = enableSsl,
    //            Timeout = 3000 // Timeout in milliseconds (10 seconds)

    //        };

    //        var mailMessage = new MailMessage
    //        {
    //            From = fromAddress,
    //            Subject = subject,
    //            Body = htmlMessage,
    //            IsBodyHtml = true,
    //        };
    //        mailMessage.To.Add(email);
    //        // This log should show if the line is reached
    //        _logger.LogInformation("Sending email...");

    //        // Attempt to send the email
    //        await client.SendMailAsync(mailMessage);

    //        // This log should show if the email was sent successfully
    //        _logger.LogInformation("Email sent successfully to {Recipient}", email);

    //    }
    //    catch (SmtpException smtpEx)
    //    {
    //        // Log SMTP-specific exceptions with detailed info
    //        _logger.LogError(smtpEx, "SMTP Error when sending email to {Recipient}: {Message}", email, smtpEx.Message);
    //    }
    //    catch (Exception ex)
    //    {
    //        // Log general exceptions
    //        _logger.LogError(ex, "General error occurred when sending email to {Recipient}: {Message}", email, ex.Message);
    //    }
    //}

    public void SendEmail(string email, string subject, string htmlMessage)
    {
        var smtpSettings = _configuration.GetSection("SmtpSettings");
        var host = smtpSettings["Host"] ?? throw new ArgumentNullException("SMTP Host is missing in configuration");
        var port = int.TryParse(smtpSettings["Port"], out var smtpPort) ? smtpPort : throw new ArgumentNullException("SMTP Port is missing in configuration");
        var enableSsl = bool.TryParse(smtpSettings["EnableSSL"], out var sslEnabled) && sslEnabled;
        var sFrom = smtpSettings["SenderEmail"] ?? throw new ArgumentNullException("SenderEmail is missing in configuration");
        var user = smtpSettings["Username"] ?? throw new ArgumentNullException("Username is missing in configuration");
        var password = smtpSettings["Password"] ?? throw new ArgumentNullException("Password is missing in configuration");

        try
        {
            using (var client = new SmtpClient(host, port))
            {
                client.Credentials = new NetworkCredential(user, password);
                client.EnableSsl = enableSsl;
                client.Timeout = 10000;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(sFrom),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                _logger.LogInformation("Attempting to send email to {Recipient}", email);
                client.Send(mailMessage); // Synchronous send
                _logger.LogInformation("Email sent successfully to {Recipient}", email);
            }
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "SMTP error occurred while sending email to {Recipient}: {Message}", email, smtpEx.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "General error occurred while sending email to {Recipient}: {Message}", email, ex.Message);
            throw;
        }
    }


    //public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    //{
    //    var smtpSettings = _configuration.GetSection("SmtpSettings");
    //    var host = smtpSettings["Host"] ?? throw new ArgumentNullException("SMTP Host is missing in configuration");
    //    var port = int.TryParse(smtpSettings["Port"], out var smtpPort) ? smtpPort : throw new ArgumentNullException("SMTP Port is missing in configuration");
    //    var enableSsl = bool.TryParse(smtpSettings["EnableSSL"], out var sslEnabled) && sslEnabled;
    //    var sFrom = smtpSettings["SenderEmail"] ?? throw new ArgumentNullException("SenderEmail is missing in configuration");
    //    var fromAddress = new MailAddress(sFrom);

    //    try
    //    {
    //        //var client = new SmtpClient(smtpSettings["Server"])
    //        //{

    //        //    Port = int.Parse(smtpSettings["Port"]),
    //        //    Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
    //        //    EnableSsl = true
    //        //};

    //        var client = new SmtpClient(host)
    //        {
    //            Port = port,
    //            Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
    //            EnableSsl = enableSsl
    //        };

    //        var mailMessage = new MailMessage
    //        {
    //            From = fromAddress,
    //            Subject = subject,
    //            Body = htmlMessage,
    //            IsBodyHtml = true,
    //        };
    //        mailMessage.To.Add(email);

    //        try
    //        {
    //             await client.SendMailAsync(mailMessage);
    //        }
    //        catch (Exception ex)
    //        {
    //            ex.Message.ToString();
    //            _logger.LogError(ex, "Error sending email :"+mailMessage.Sender);

    //        }
    //        _logger.LogError("Sending Registration to Email :"+sFrom);

    //    }
    //    catch (Exception ex)
    //    {
    //        ex.Message.ToString();
    //        _logger.LogError(ex, "Error sending email");

    //    }
    //}

    //public void SendEmail(string email, string subject, string htmlMessage)
    //{
    //    var smtpSettings = _configuration.GetSection("SmtpSettings");
    //    var host = smtpSettings["Host"] ?? throw new ArgumentNullException("SMTP Host is missing in configuration");
    //    var port = int.TryParse(smtpSettings["Port"], out var smtpPort) ? smtpPort : throw new ArgumentNullException("SMTP Port is missing in configuration");
    //    var enableSsl = bool.TryParse(smtpSettings["EnableSSL"], out var sslEnabled) && sslEnabled;
    //    var sFrom = smtpSettings["SenderEmail"] ?? throw new ArgumentNullException("SenderEmail is missing in configuration");
    //    var user = smtpSettings["Username"] ?? throw new ArgumentNullException("Username is missing in configuration");
    //    var Password = smtpSettings["Password"] ?? throw new ArgumentNullException("Password is missing in configuration");

    //    try
    //    {
    //        var client = new SmtpClient(host, port);
    //        //using (var client = new SmtpClient(host, port))
    //        //{
    //        try
    //        {
    //            client.Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]);
    //            client.EnableSsl = enableSsl;
    //            client.Timeout = 10000;

    //            var mailMessage = new MailMessage();
    //            //var mailMessage = new MailMessage
    //            //{
    //            mailMessage.From = new MailAddress(sFrom);
    //            mailMessage.Subject = subject;
    //            mailMessage.Body = htmlMessage;
    //            mailMessage.IsBodyHtml = true;
    //            mailMessage.To.Add(email);


    //            //};
    //            mailMessage.To.Add(email);

    //            _logger.LogInformation("Attempting to send email to {Recipient}", email);
    //            client.Send(mailMessage); // Synchronous send
    //            _logger.LogInformation("Email sent successfully to {Recipient}", email);
    //        }
    //        catch (Exception ex)
    //        {
    //            ex.Message.ToString();
    //            _logger.LogError(ex, "SMTP error occurred while sending email to {Recipient}: {Message}", email, ex.Message);
    //            throw;
    //        }
    //        //}
    //    }
    //    catch (SmtpException smtpEx)
    //    {
    //        _logger.LogError(smtpEx, "SMTP error occurred while sending email to {Recipient}: {Message}", email, smtpEx.Message);
    //        throw;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "General error occurred while sending email to {Recipient}: {Message}", email, ex.Message);
    //        throw;
    //    }
    //}


}
