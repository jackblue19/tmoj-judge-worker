namespace Infrastructure.Configurations.Auth;

public class EmailSettings
{
    public string FromEmail { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string BrevoApiKey { get; set; } = string.Empty;
    public string VerificationEmailTemplate { get; set; } = string.Empty;
    public string ForgotPasswordEmailTemplate { get; set; } = string.Empty;
    public string ChangePasswordEmailTemplate { get; set; } = string.Empty;
}
