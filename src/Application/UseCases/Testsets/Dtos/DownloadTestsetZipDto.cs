namespace Application.UseCases.Testsets.Dtos;

public sealed class DownloadTestsetZipDto
{
    public string FileName { get; init; } = null!;
    public string ContentType { get; init; } = "application/zip";
    public byte[] Bytes { get; init; } = [];
}