using System;
using System.Collections.Generic;
using System.Threading;
using Application.Abstractions.Outbound.Services;
using Infrastructure.Configurations.Auth;
using Microsoft.Extensions.Options;
using brevo_csharp.Api;
using brevo_csharp.Model;

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
        if (!brevo_csharp.Client.Configuration.Default.ApiKey.ContainsKey("api-key"))
        {
            brevo_csharp.Client.Configuration.Default.ApiKey.Add("api-key", _settings.BrevoApiKey);
        }
        else
        {
            brevo_csharp.Client.Configuration.Default.ApiKey["api-key"] = _settings.BrevoApiKey;
        }

        var apiInstance = new TransactionalEmailsApi();

        var sendSmtpEmail = new SendSmtpEmail(
            sender: new SendSmtpEmailSender(email: _settings.FromEmail, name: _settings.DisplayName),
            to: new List<SendSmtpEmailTo> { new SendSmtpEmailTo(email: to) },
            htmlContent: body,
            subject: subject
        );

        // Tạm thời tắt gửi mail theo yêu cầu:
        // await apiInstance.SendTransacEmailAsync(sendSmtpEmail);
        await System.Threading.Tasks.Task.CompletedTask;
    }
}
