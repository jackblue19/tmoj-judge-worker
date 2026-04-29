namespace Application.Abstractions.Outbound.Services;

public interface ICloudinaryService
{
    /// <summary>
    /// Upload an avatar image to Cloudinary.
    /// A new GUID is generated as the PublicId, stored in the "avatars" folder.
    /// </summary>
    /// <param name="fileStream">The image content stream.</param>
    /// <param name="fileExtension">File extension including dot, e.g. ".png", ".jpg".</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The GUID used as the public ID on Cloudinary.</returns>
    Task<Guid> UploadAvatarAsync(Stream fileStream, string fileExtension, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replace an existing avatar on Cloudinary (same GUID / PublicId, new image).
    /// </summary>
    /// <param name="avatarId">The existing GUID (PublicId) of the avatar.</param>
    /// <param name="fileStream">The new image content stream.</param>
    /// <param name="fileExtension">File extension including dot, e.g. ".png", ".jpg".</param>
    /// <param name="cancellationToken"></param>
    Task ReplaceAvatarAsync(Guid avatarId, Stream fileStream, string fileExtension, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an avatar from Cloudinary by its GUID.
    /// </summary>
    Task<bool> DeleteAvatarAsync(Guid avatarId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve a GUID to the full Cloudinary URL for display.
    /// </summary>
    /// <param name="avatarId">The GUID used as the public ID.</param>
    /// <returns>The full public URL, or null if avatarId is empty.</returns>
    string? GetAvatarUrl(Guid avatarId);

    // Generic methods for other images (e.g. store items)
    Task<Guid> UploadImageAsync(Stream fileStream, string fileExtension, string folder = "items", CancellationToken cancellationToken = default);
    Task ReplaceImageAsync(Guid imageId, Stream fileStream, string fileExtension, string folder = "items", CancellationToken cancellationToken = default);
    Task<bool> DeleteImageAsync(Guid imageId, string folder = "items", CancellationToken cancellationToken = default);
    string? GetImageUrl(Guid imageId, string folder = "items");
}
