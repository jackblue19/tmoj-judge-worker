namespace Infrastructure.Configurations.Auth;

public class GoogleOptions
{
    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
    public List<string> AllowedDomains { get; set; } = new();
}
