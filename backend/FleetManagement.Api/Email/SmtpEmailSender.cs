using System.Net;
using System.Net.Mail;

namespace FleetManagement.Api.Email;

public interface IEmailSender
{
  Task SendAsync(string toEmail, string subject, string body, bool isHtml = false);
}

public class SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger) : IEmailSender
{
  public async Task SendAsync(string toEmail, string subject, string body, bool isHtml = false)
  {
    var host = configuration["Smtp:Host"];
    var username = configuration["Smtp:UserName"];
    var password = configuration["Smtp:Password"];
    var fromEmail = configuration["Smtp:FromEmail"] ?? username;
    var fromName = configuration["Smtp:FromName"] ?? "FleetManager";

    if (string.IsNullOrWhiteSpace(host) ||
        string.IsNullOrWhiteSpace(username) ||
        string.IsNullOrWhiteSpace(password) ||
        string.IsNullOrWhiteSpace(fromEmail))
    {
      throw new InvalidOperationException("SMTP email is not configured.");
    }

    var port = int.TryParse(configuration["Smtp:Port"], out var parsedPort) ? parsedPort : 587;
    var enableSsl = !bool.TryParse(configuration["Smtp:EnableSsl"], out var parsedSsl) || parsedSsl;

    using var message = new MailMessage
    {
      From = new MailAddress(fromEmail, fromName),
      Subject = subject,
      Body = body,
      IsBodyHtml = isHtml
    };
    message.To.Add(toEmail);

    using var client = new SmtpClient(host, port)
    {
      EnableSsl = enableSsl,
      Credentials = new NetworkCredential(username, password)
    };

    logger.LogInformation("Sending email to {Email} with subject {Subject}.", toEmail, subject);
    await client.SendMailAsync(message);
  }
}
