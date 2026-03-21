using System.ComponentModel.DataAnnotations;

namespace WebAPI.Controllers.v1.ProblemManagement;

public class ProblemDtos
{
}

public sealed class ProblemCreateDto
{
    public string? Slug { get; set; }

    [Required]
    public string Title { get; set; } = null!;

    public string? Difficulty { get; set; }
    public string? TypeCode { get; set; }
    public string? VisibilityCode { get; set; }
    public string? ScoringCode { get; set; }

    public string? DescriptionMd { get; set; }

    public decimal? AcceptancePercent { get; set; }

    public int? DisplayIndex { get; set; }

    public int? TimeLimitMs { get; set; }
    public int? MemoryLimitKb { get; set; }

    public Guid? CreatedBy { get; set; }
}

public sealed class ProblemUpdateDto
{
    [Required]
    public Guid Id { get; set; }

    public string? Slug { get; set; }
    public string? Title { get; set; }
    public string? Difficulty { get; set; }
    public string? TypeCode { get; set; }
    public string? VisibilityCode { get; set; }
    public string? ScoringCode { get; set; }
    public string? DescriptionMd { get; set; }
    public decimal? AcceptancePercent { get; set; }
    public int? DisplayIndex { get; set; }
    public int? TimeLimitMs { get; set; }
    public int? MemoryLimitKb { get; set; }

    public string? StatusCode { get; set; } // draft/pending/published/archived

    public Guid? UpdatedBy { get; set; }
}
public sealed class ProblemResponseDto
{
    public Guid Id { get; set; }
    public string? Slug { get; set; }
    public string Title { get; set; } = null!;
    public string? Difficulty { get; set; }
    public string? StatusCode { get; set; }
    public bool IsActive { get; set; }
    public string? Content { get; set; }

    public decimal? AcceptancePercent { get; set; }

    public int? TimeLimitMs { get; set; }
    public int? MemoryLimitKb { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public sealed class ProblemSetDifficultyDto
{
    [Required]
    public string Difficulty { get; set; } = null!;

    public Guid? UpdatedBy { get; set; }
}

public sealed class ProblemPublishDto
{
    public Guid? PublishedBy { get; set; }
}

public sealed class ProblemArchiveDto
{
    public Guid? ArchivedBy { get; set; }
}

