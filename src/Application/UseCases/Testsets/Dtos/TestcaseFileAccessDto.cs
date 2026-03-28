namespace Application.UseCases.Testsets.Dtos;

public sealed class TestcaseFileAccessDto
{
    public string Mode { get; init; } = null!; // "redirect"
    public string Url { get; init; } = null!;
    public string ObjectKey { get; init; } = null!;
    public string ContentType { get; init; } = "application/octet-stream";
    public string FileName { get; init; } = null!;
}