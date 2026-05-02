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
            _logger.LogInformation("Email sending skipped because EmailSettings.Enabled is false. To={To}, Subject={Subject}" , to , subject);
            return;
        }

        if ( string.IsNullOrWhiteSpace(_settings.FromEmail) )
            throw new InvalidOperationException("EmailSettings.FromEmail is required.");

        if ( string.IsNullOrWhiteSpace(to) )
            throw new ArgumentException("Recipient email is required." , nameof(to));

        var from = BuildFromAddress(_settings.DisplayName , _settings.FromEmail);

        var request = new SendEmailRequest
        {
            FromEmailAddress = from ,
            Destination = new Destination
            {
                ToAddresses = new List<string> { to }
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

        if ( !string.IsNullOrWhiteSpace(_settings.ReplyToEmail) )
            request.ReplyToAddresses = new List<string> { _settings.ReplyToEmail };

        if ( !string.IsNullOrWhiteSpace(_settings.ConfigurationSetName) )
            request.ConfigurationSetName = _settings.ConfigurationSetName;

        var response = await _ses.SendEmailAsync(request , cancellationToken);

        _logger.LogInformation(
            "SES email sent. MessageId={MessageId}, To={To}, Subject={Subject}" ,
            response.MessageId ,
            to ,
            subject);
    }

    private static string BuildFromAddress(string displayName , string email)
    {
        if ( string.IsNullOrWhiteSpace(displayName) )
            return email;

        var safeName = displayName.Replace("\"" , string.Empty).Trim();
        return $"\"{safeName}\" <{email}>";
    }

    private static string ToPlainText(string html)
    {
        if ( string.IsNullOrWhiteSpace(html) )
            return string.Empty;

        var text = Regex.Replace(html , "<br\\s*/?>" , "\n" , RegexOptions.IgnoreCase);
        text = Regex.Replace(text , "</p>" , "\n" , RegexOptions.IgnoreCase);
        text = Regex.Replace(text , "<.*?>" , string.Empty);

        return WebUtility.HtmlDecode(text).Trim();
    }
}