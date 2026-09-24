using System.Net;
using System.Net.Mail;

namespace MentalHealth.API.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        var host = _config["Smtp:Host"];
        if (string.IsNullOrEmpty(host))
        {
            _logger.LogWarning("SMTP non configuré, email non envoyé : {To} - {Subject}", to, subject);
            return;
        }

        try
        {
            using var client = new SmtpClient(host, int.Parse(_config["Smtp:Port"] ?? "587"))
            {
                Credentials = new NetworkCredential(_config["Smtp:User"], _config["Smtp:Password"]),
                EnableSsl = true
            };

            var mail = new MailMessage(_config["Smtp:From"] ?? "noreply@mindcare.app", to, subject, body);
            await client.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Échec envoi email à {To}", to);
        }
    }
}