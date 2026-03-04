using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace WebAPI.Controllers.v1.ProblemManagement;

public sealed class ProblemUploadRequestDto
{
    public Guid? RequestedBy { get; set; }
    public string? Purpose { get; set; } // "testset" | "testcases"
}

public sealed class ProblemUploadRequestResponseDto
{
    public string UploadSessionId { get; set; } = null!;
    public DateTime ExpiresAtUtc { get; set; }
}

public sealed class ProblemTestsetCreateDto
{
    [Required]
    public string Type { get; set; } = null!; // testsets.type NOT NULL

    public string? Note { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? ExpireAt { get; set; }
}

public sealed class ProblemTestsetResponseDto
{
    public Guid Id { get; set; }
    public Guid ProblemId { get; set; }
    public string Type { get; set; } = null!;
    public bool IsActive { get; set; }
    public string? Note { get; set; }
    public Guid? StorageBlobId { get; set; }
    public DateTime? ExpireAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class UploadTestcasesMetaDto
{
    [Required]
    public Guid TestsetId { get; set; }

    public bool ReplaceExisting { get; set; } = false;

    public Guid? UploadedBy { get; set; }
    public DateTime? ExpireAt { get; set; }
}

public sealed class UploadTestcasesFormDto
{
    [Required]
    [FromForm(Name = "testsetId")]
    public Guid TestsetId { get; set; }

    [FromForm(Name = "replaceExisting")]
    public bool ReplaceExisting { get; set; } = false;

    [Required]
    [FromForm(Name = "file")]
    public IFormFile File { get; set; } = null!;
}


public sealed class DeleteTestcaseRangeDto
{
    [Range(1 , 999)]
    public int From { get; set; }

    [Range(1 , 999)]
    public int To { get; set; }
}

public sealed class AddSingleTestcaseFormDto
{
    [Range(1 , 999)]
    public int Ordinal { get; set; }

    public bool Overwrite { get; set; } = false;

    [Required]
    public IFormFile Input { get; set; } = null!;

    [Required]
    public IFormFile Output { get; set; } = null!;
}

public sealed class ProblemCreateFormDto
{
    public string Slug { get; set; } = null!;
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

    public IFormFile? DescriptionFile { get; set; }
}