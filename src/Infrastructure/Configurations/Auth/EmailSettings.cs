namespace Infrastructure.Configurations.Auth;

public sealed class EmailSettings
{
    public string Provider { get; set; } = "ses";

    public bool Enabled { get; set; } = true;

    public string FromEmail { get; set; } = string.Empty;

    public string DisplayName { get; set; } = "TMOJ";

    public string? ReplyToEmail { get; set; }

    public string? ConfigurationSetName { get; set; }

    public string FrontendBaseUrl { get; set; } = string.Empty;

    public string ApiBaseUrl { get; set; } = string.Empty;

    public string VerificationEmailTemplate { get; set; } = string.Empty;

    public string ForgotPasswordEmailTemplate { get; set; } = string.Empty;

    public string ChangePasswordEmailTemplate { get; set; } = string.Empty;
}