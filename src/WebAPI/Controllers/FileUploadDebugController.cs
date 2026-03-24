using Application.Abstractions.Outbound.Services;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/debug/files")]
public sealed class FileUploadDebugController : ControllerBase
{
    private readonly IR2Service _r2;
    private readonly ICloudinaryService _cloudinary;

    public FileUploadDebugController(IR2Service r2, ICloudinaryService cloudinary)
    {
        _r2 = r2;
        _cloudinary = cloudinary;
    }

    // ─── R2: Upload (dùng GUID có sẵn) ──────────────────────────────

    /// <summary>
    /// Upload a file to the Testset R2 bucket using an existing GUID as the object key.
    /// </summary>
    [HttpPost("r2/testset/{id}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50_000_000)] // 50 MB
    public async Task<IActionResult> UploadTestset(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { Message = "File is required." });

        var ext = Path.GetExtension(file.FileName);
        var objectKey = $"{id}{ext}";
        await using var stream = file.OpenReadStream();
        await _r2.UploadAsync("Testset", objectKey, stream, file.ContentType, ct);

        return Ok(new { Message = "Testset file uploaded.", Id = id, ObjectKey = objectKey });
    }

    /// <summary>
    /// Upload a file to the Problem R2 bucket using an existing GUID (problemId) as the object key.
    /// </summary>
    [HttpPost("r2/problem/{id}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadProblem(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { Message = "File is required." });

        var ext = Path.GetExtension(file.FileName);
        var objectKey = $"{id}{ext}";
        await using var stream = file.OpenReadStream();
        await _r2.UploadAsync("Problem", objectKey, stream, file.ContentType, ct);

        return Ok(new { Message = "Problem file uploaded.", Id = id, ObjectKey = objectKey });
    }

    /// <summary>
    /// Upload a file to the Submission R2 bucket using an existing GUID (submissionId) as the object key.
    /// </summary>
    [HttpPost("r2/submission/{id}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadSubmission(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { Message = "File is required." });

        var ext = Path.GetExtension(file.FileName);
        var objectKey = $"{id}{ext}";
        await using var stream = file.OpenReadStream();
        await _r2.UploadAsync("Submission", objectKey, stream, file.ContentType, ct);

        return Ok(new { Message = "Submission file uploaded.", Id = id, ObjectKey = objectKey });
    }

    // ─── R2: Download ────────────────────────────────────────────────

    [HttpGet("r2/testset/{id}")]
    public async Task<IActionResult> DownloadTestset(Guid id, [FromQuery] string ext = ".txt", CancellationToken ct = default)
    {
        var objectKey = $"{id}{ext}";
        var stream = await _r2.DownloadAsync("Testset", objectKey, ct);
        return File(stream, "application/octet-stream", objectKey);
    }

    [HttpGet("r2/problem/{id}")]
    public async Task<IActionResult> DownloadProblem(Guid id, [FromQuery] string ext = ".txt", CancellationToken ct = default)
    {
        var objectKey = $"{id}{ext}";
        var stream = await _r2.DownloadAsync("Problem", objectKey, ct);
        return File(stream, "application/octet-stream", objectKey);
    }

    [HttpGet("r2/submission/{id}")]
    public async Task<IActionResult> DownloadSubmission(Guid id, [FromQuery] string ext = ".txt", CancellationToken ct = default)
    {
        var objectKey = $"{id}{ext}";
        var stream = await _r2.DownloadAsync("Submission", objectKey, ct);
        return File(stream, "application/octet-stream", objectKey);
    }

    // ─── R2: Delete ──────────────────────────────────────────────────

    [HttpDelete("r2/testset/{id}")]
    public async Task<IActionResult> DeleteTestset(Guid id, [FromQuery] string ext = ".txt", CancellationToken ct = default)
    {
        var objectKey = $"{id}{ext}";
        await _r2.DeleteAsync("Testset", objectKey, ct);
        return Ok(new { Message = "Testset file deleted.", Id = id, ObjectKey = objectKey });
    }

    [HttpDelete("r2/problem/{id}")]
    public async Task<IActionResult> DeleteProblem(Guid id, [FromQuery] string ext = ".txt", CancellationToken ct = default)
    {
        var objectKey = $"{id}{ext}";
        await _r2.DeleteAsync("Problem", objectKey, ct);
        return Ok(new { Message = "Problem file deleted.", Id = id, ObjectKey = objectKey });
    }

    [HttpDelete("r2/submission/{id}")]
    public async Task<IActionResult> DeleteSubmission(Guid id, [FromQuery] string ext = ".txt", CancellationToken ct = default)
    {
        var objectKey = $"{id}{ext}";
        await _r2.DeleteAsync("Submission", objectKey, ct);
        return Ok(new { Message = "Submission file deleted.", Id = id, ObjectKey = objectKey });
    }

    // ─── R2: Pre-signed URL ─────────────────────────────────────────

    [HttpGet("r2/presigned-url")]
    public async Task<IActionResult> GetPresignedUrl(
        [FromQuery] string bucket,
        [FromQuery] Guid id,
        [FromQuery] string ext = ".txt",
        [FromQuery] int expiresInMinutes = 60,
        CancellationToken ct = default)
    {
        var objectKey = $"{id}{ext}";
        var url = await _r2.GetPresignedUrlAsync(bucket, objectKey, TimeSpan.FromMinutes(expiresInMinutes), ct);
        return Ok(new { Url = url, ObjectKey = objectKey, ExpiresInMinutes = expiresInMinutes });
    }

    // ─── Cloudinary: Avatar (dùng userId làm publicId) ──────────────

    /// <summary>
    /// Upload an avatar image to Cloudinary using an existing GUID (userId) as the public ID.
    /// Supports automatic compression for images over 5MB.
    /// </summary>
    [HttpPost("cloudinary/avatar/{userId}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50_000_000)] // 50 MB limits the HTTP request
    public async Task<IActionResult> UploadCloudinaryAvatar(Guid userId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { Message = "File is required." });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { Message = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}" });

        await using var stream = file.OpenReadStream();
        Stream uploadStream = stream;

        // Giảm dung lượng ảnh xuống dưới 5MB nếu cần
        if (file.Length > 5_000_000)
        {
            var memoryStream = new MemoryStream();
            using var image = await Image.LoadAsync(stream, ct);
            
            // Resize if dimensions are excessively large
            if (image.Width > 2048 || image.Height > 2048)
            {
                var options = new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(2048, 2048)
                };
                image.Mutate(x => x.Resize(options));
            }
            
            // Save as Jpeg with quality 75 to reduce size
            var encoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 75 };
            await image.SaveAsync(memoryStream, encoder, ct);
            memoryStream.Position = 0;
            uploadStream = memoryStream;
            ext = ".jpg"; // Update extension to reflect new format
        }

        // Use ReplaceAvatarAsync which uploads with the given GUID as publicId
        await _cloudinary.ReplaceAvatarAsync(userId, uploadStream, ext, ct);
        var avatarUrl = _cloudinary.GetAvatarUrl(userId);

        if (uploadStream != stream)
            await uploadStream.DisposeAsync();

        return Ok(new { Message = "Avatar uploaded.", UserId = userId, AvatarUrl = avatarUrl });
    }

    /// <summary>
    /// Delete an avatar from Cloudinary by userId GUID.
    /// </summary>
    [HttpDelete("cloudinary/avatar/{userId}")]
    public async Task<IActionResult> DeleteCloudinaryAvatar(Guid userId, CancellationToken ct)
    {
        await _cloudinary.DeleteAvatarAsync(userId, ct);
        return Ok(new { Message = "Avatar deleted.", UserId = userId });
    }

    /// <summary>
    /// Get the Cloudinary avatar URL for a given userId GUID.
    /// </summary>
    [HttpGet("cloudinary/avatar/{userId}")]
    public IActionResult GetCloudinaryAvatarUrl(Guid userId)
    {
        var url = _cloudinary.GetAvatarUrl(userId);
        return Ok(new { UserId = userId, AvatarUrl = url });
    }
}
