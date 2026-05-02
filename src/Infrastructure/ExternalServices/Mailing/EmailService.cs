using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Application.Abstractions.Outbound.Services;
using Infrastructure.Configurations.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.RegularExpressions;

namespace Infrastructure.ExternalServices.Mailing;

public sealed class SesEmailService : IEmailService
{
    private readonly IAmazonSimpleEmailServiceV2 _ses;
    private readonly EmailSettings _settings;
    private readonly ILogger<SesEmailService> _logger;

    public SesEmailService(
        IAmazonSimpleEmailServiceV2 ses ,
        IOptionsSnapshot<EmailSettings> settings ,
        ILogger<SesEmailService> logger)
    {
        _ses = ses;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string to ,
        string subject ,
        string body ,
        CancellationToken cancellationToken = default)
    {
        if ( !_settings.Enabled )
        {
            _logger.LogInformation(
                "Email sending skipped because EmailSettings.Enabled is false. To={To}, Subject={Subject}" ,
                to ,
                subject);

            return;
        }

        var fromEmail = NormalizeEmail(_settings.FromEmail);
        var toEmail = NormalizeEmail(to);
        var replyToEmail = NormalizeNullableEmail(_settings.ReplyToEmail);
        var configurationSetName = NormalizeNullableText(_settings.ConfigurationSetName);

        if ( string.IsNullOrWhiteSpace(fromEmail) )
            throw new InvalidOperationException("EmailSettings.FromEmail is required.");

        if ( string.IsNullOrWhiteSpace(toEmail) )
            throw new ArgumentException("Recipient email is required." , nameof(to));

        // IMPORTANT:
        // Để tránh IAM/SES hiểu nhầm identity khi test, KHÔNG dùng:
        // "TMOJ" <no-reply@tmoj.id.vn>
        // Mà gửi thẳng email address.
        var sourceEmailAddress = fromEmail;

        _logger.LogInformation(
            "SES sending email. Source={SourceEmailAddress}, ReplyTo={ReplyToEmail}, To={ToEmail}, ConfigurationSet={ConfigurationSetName}, Subject={Subject}" ,
            sourceEmailAddress ,
            replyToEmail ,
            toEmail ,
            configurationSetName ,
            subject);

        var request = new SendEmailRequest
        {
            FromEmailAddress = sourceEmailAddress ,

            Destination = new Destination
            {
                ToAddresses = new List<string>
                {
                    toEmail
                }
            } ,

            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content
                    {
                        Charset = "UTF-8" ,
                        Data = subject
                    } ,

                    Body = new Body
                    {
                        Html = new Content
                        {
                            Charset = "UTF-8" ,
                            Data = body
                        } ,

                        Text = new Content
                        {
                            Charset = "UTF-8" ,
                            Data = ToPlainText(body)
                        }
                    }
                }
            }
        };

        if ( !string.IsNullOrWhiteSpace(replyToEmail) )
        {
            request.ReplyToAddresses = new List<string>
            {
                replyToEmail
            };
        }

        if ( !string.IsNullOrWhiteSpace(configurationSetName) )
        {
            request.ConfigurationSetName = configurationSetName;
        }

        try
        {
            var response = await _ses.SendEmailAsync(request , cancellationToken);

            _logger.LogInformation(
                "SES email sent successfully. MessageId={MessageId}, Source={SourceEmailAddress}, To={ToEmail}, Subject={Subject}" ,
                response.MessageId ,
                sourceEmailAddress ,
                toEmail ,
                subject);
        }
        catch ( AmazonSimpleEmailServiceV2Exception ex )
        {
            _logger.LogError(
                ex ,
                "SES email failed. ErrorCode={ErrorCode}, StatusCode={StatusCode}, Source={SourceEmailAddress}, ReplyTo={ReplyToEmail}, To={ToEmail}, ConfigurationSet={ConfigurationSetName}, Subject={Subject}" ,
                ex.ErrorCode ,
                ex.StatusCode ,
                sourceEmailAddress ,
                replyToEmail ,
                toEmail ,
                configurationSetName ,
                subject);

            throw;
        }
    }

    private static string NormalizeEmail(string? email)
    {
        return email?.Trim() ?? string.Empty;
    }

    private static string? NormalizeNullableEmail(string? email)
    {
        if ( string.IsNullOrWhiteSpace(email) )
            return null;

        return email.Trim();
    }

    private static string? NormalizeNullableText(string? value)
    {
        if ( string.IsNullOrWhiteSpace(value) )
            return null;

        return value.Trim();
    }

    private static string ToPlainText(string html)
    {
        if ( string.IsNullOrWhiteSpace(html) )
            return string.Empty;

        var text = html;

        text = Regex.Replace(
            text ,
            "<br\\s*/?>" ,
            "\n" ,
            RegexOptions.IgnoreCase);

        text = Regex.Replace(
            text ,
            "</p>" ,
            "\n" ,
            RegexOptions.IgnoreCase);

        text = Regex.Replace(
            text ,
            "<.*?>" ,
            string.Empty ,
            RegexOptions.Singleline);

        return WebUtility.HtmlDecode(text).Trim();
    }
}