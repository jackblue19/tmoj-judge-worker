namespace Infrastructure.Configurations.Auth;

public class EmailSettings
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string VerificationEmailTemplate { get; set; } = string.Empty;
}
