namespace Infrastructure.Configurations.Aws;

public sealed class AwsSettings
{
    public string Region { get; set; } = "ap-southeast-1";

    public string AccessKeyId { get; set; } = string.Empty;

    public string SecretAccessKey { get; set; } = string.Empty;
}