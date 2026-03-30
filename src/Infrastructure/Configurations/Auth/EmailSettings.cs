namespace Infrastructure.Configurations.Auth;

public class EmailSettings
{
    public string Email { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string VerificationEmailTemplate { get; set; } = string.Empty;
    public string ForgotPasswordEmailTemplate { get; set; } = string.Empty;
    public string ChangePasswordEmailTemplate { get; set; } = string.Empty;
}
