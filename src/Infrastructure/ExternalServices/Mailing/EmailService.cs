using Application.Abstractions.Outbound.Services;
using Infrastructure.Configurations.Auth;
using Microsoft.Extensions.Options;
using System.Threading;

namespace Infrastructure.ExternalServices.Mailing;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptionsSnapshot<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async System.Threading.Tasks.Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        using var client = new System.Net.Mail.SmtpClient(_settings.Host)
        {
            Port = _settings.Port,
            Credentials = new System.Net.NetworkCredential(_settings.Email, _settings.Password),
            EnableSsl = true,
        };

        var mailMessage = new System.Net.Mail.MailMessage
        {
            From = new System.Net.Mail.MailAddress(_settings.FromEmail, _settings.DisplayName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
        };

        mailMessage.To.Add(to);

        await client.SendMailAsync(mailMessage);
    }
}
