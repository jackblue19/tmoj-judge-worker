using Application.Abstractions.Outbound.Services;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Infrastructure.Configurations.FileStorage;
using Microsoft.Extensions.Options;

namespace Infrastructure.ExternalServices.FileStorage;

public class CloudinaryService : ICloudinaryService
{
    private const string AvatarFolder = "avatars";

    private readonly Cloudinary _cloudinary;
    private readonly string _cloudName;

    public CloudinaryService(IOptions<CloudinarySettings> settings)
    {
        var s = settings.Value;
        _cloudName = s.CloudName;
        var account = new Account(s.CloudName, s.ApiKey, s.ApiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<Guid> UploadAvatarAsync(Stream fileStream, string fileExtension, CancellationToken cancellationToken = default)
    {
        var avatarId = Guid.NewGuid();
        var publicId = $"{AvatarFolder}/{avatarId}";

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription($"{avatarId}{fileExtension}", fileStream),
            PublicId = publicId,
            Overwrite = true,
            Transformation = new Transformation()
                .Width(400).Height(400).Crop("fill").Gravity("face")
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (result.Error != null)
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");

        return avatarId;
    }

    public async Task ReplaceAvatarAsync(Guid avatarId, Stream fileStream, string fileExtension, CancellationToken cancellationToken = default)
    {
        var publicId = $"{AvatarFolder}/{avatarId}";

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription($"{avatarId}{fileExtension}", fileStream),
            PublicId = publicId,
            Overwrite = true,
            Invalidate = true,
            Transformation = new Transformation()
                .Width(400).Height(400).Crop("fill").Gravity("face")
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (result.Error != null)
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");
    }

    public async Task<bool> DeleteAvatarAsync(Guid avatarId, CancellationToken cancellationToken = default)
    {
        var publicId = $"{AvatarFolder}/{avatarId}";
        var deleteParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deleteParams);
        return result.Result == "ok";
    }

    public string? GetAvatarUrl(Guid avatarId)
    {
        if (avatarId == Guid.Empty)
            return null;

        // Build the standard Cloudinary URL from the public ID
        // Format: https://res.cloudinary.com/{cloud_name}/image/upload/{folder}/{public_id}
        return _cloudinary.Api.UrlImgUp
            .BuildUrl($"{AvatarFolder}/{avatarId}");
    }

    // =====================================================
    // GENERIC METHODS
    // =====================================================

    public async Task<Guid> UploadImageAsync(Stream fileStream, string fileExtension, string folder = "items", CancellationToken cancellationToken = default)
    {
        var imageId = Guid.NewGuid();
        var publicId = $"{folder}/{imageId}";

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription($"{imageId}{fileExtension}", fileStream),
            PublicId = publicId,
            Overwrite = true
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (result.Error != null)
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");

        return imageId;
    }

    public async Task ReplaceImageAsync(Guid imageId, Stream fileStream, string fileExtension, string folder = "items", CancellationToken cancellationToken = default)
    {
        var publicId = $"{folder}/{imageId}";

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription($"{imageId}{fileExtension}", fileStream),
            PublicId = publicId,
            Overwrite = true,
            Invalidate = true
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (result.Error != null)
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");
    }

    public async Task<bool> DeleteImageAsync(Guid imageId, string folder = "items", CancellationToken cancellationToken = default)
    {
        var publicId = $"{folder}/{imageId}";
        var deleteParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deleteParams);
        return result.Result == "ok";
    }

    public string? GetImageUrl(Guid imageId, string folder = "items")
    {
        if (imageId == Guid.Empty)
            return null;

        return _cloudinary.Api.UrlImgUp
            .BuildUrl($"{folder}/{imageId}");
    }
}
