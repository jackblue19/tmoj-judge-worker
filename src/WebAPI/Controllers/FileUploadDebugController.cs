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

    // ─── R2: API ─────────────────────────────────────────────────────────────

    [HttpPost("r2/{type}/{id}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadR2(string type, Guid id, IFormFile file, [FromQuery] bool replaceIfExists = false, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { Message = "File is required." });

        var ext = Path.GetExtension(file.FileName);
        
        await using var stream = file.OpenReadStream();
        
        if (replaceIfExists)
        {
            await _r2.ReplaceIfExistsAsync(type, id, ext, stream, file.ContentType, ct);
        }
        else
        {
            await _r2.UploadAsync(type, id, ext, stream, file.ContentType, ct);
        }

        return Ok(new { Message = "File uploaded successfully.", Id = id });
    }

    [HttpGet("r2/download/{type}/{id}")]
    public async Task<IActionResult> GetR2DownloadUrl(string type, Guid id, [FromQuery] int? expiresInMinutes, CancellationToken ct = default)
    {
        var url = await _r2.GetPresignedUrlForDownloadAsync(type, id, expiresInMinutes, ct);
        
        if (url == null) 
            return NotFound(new { Message = $"File with ID {id} not found." });

        return Ok(new { Url = url });
    }

    [HttpGet("r2/view/{type}/{id}")]
    public async Task<IActionResult> GetR2ViewUrl(string type, Guid id, [FromQuery] int? expiresInMinutes, CancellationToken ct = default)
    {
        var url = await _r2.GetPresignedUrlForViewAsync(type, id, expiresInMinutes, ct);
        
        if (url == null) 
            return NotFound(new { Message = $"File with ID {id} not found." });

        return Ok(new { Url = url });
    }

    [HttpGet("r2/public/{type}/{id}")]
    public async Task<IActionResult> GetR2PublicUrl(string type, Guid id, CancellationToken ct = default)
    {
        var url = await _r2.GetPublicUrlAsync(type, id, ct);
        
        if (url == null) 
            return NotFound(new { Message = $"File with ID {id} not found." });

        return Ok(new { Url = url });
    }

    [HttpDelete("r2/{type}/{id}")]
    public async Task<IActionResult> DeleteR2(string type, Guid id, CancellationToken ct = default)
    {
        var fullKey = await _r2.DeleteAsync(type, id, ct);
        
        if (fullKey == null) 
            return NotFound(new { Message = $"File with ID {id} not found." });

        return Ok(new { Message = "File deleted successfully.", Id = id, ObjectKey = fullKey });
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
