using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace WebAPI.Controllers.v2.TestsetManagement;

public sealed class UploadTestcasesFormDto
{
    [Required]
    [FromForm(Name = "testsetId")]
    public Guid TestsetId { get; set; }

    [FromForm(Name = "replaceExisting")]
    public bool ReplaceExisting { get; set; } = true;

    [Required]
    [FromForm(Name = "file")]
    public IFormFile File { get; set; } = null!;
}
