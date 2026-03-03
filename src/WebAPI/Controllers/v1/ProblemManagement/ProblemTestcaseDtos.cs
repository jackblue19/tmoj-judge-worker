namespace WebAPI.Controllers.v1.ProblemManagement;

public sealed class TestcaseItemDto
{
    public int Ordinal { get; set; }             // 1,2,3...
    public string FolderName { get; set; } = null!; // "001"
    public string InputFileName { get; set; } = null!;
    public string OutputFileName { get; set; } = null!;
    public long InputSizeBytes { get; set; }
    public long OutputSizeBytes { get; set; }

    public string? InputPreview { get; set; }    // optional
    public string? OutputPreview { get; set; }   // optional
}

public sealed class TestcaseListDto
{
    public Guid ProblemId { get; set; }
    public Guid TestsetId { get; set; }
    public string Slug { get; set; } = null!;
    public string RootPath { get; set; } = null!;
    public int Total { get; set; }
    public List<TestcaseItemDto> Items { get; set; } = new();
}

public sealed class TestsetDto
{
    public Guid Id { get; set; }
    public Guid ProblemId { get; set; }

    public string? Type { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class TestsetListDto
{
    public Guid ProblemId { get; set; }
    public int Total { get; set; }
    public List<TestsetDto> Items { get; set; } = new();
}