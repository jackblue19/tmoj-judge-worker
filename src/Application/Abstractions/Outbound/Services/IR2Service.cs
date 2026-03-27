namespace Application.Abstractions.Outbound.Services;

public interface IR2Service
{
    /// <summary>
    /// Uploads a file to the Cloudflare R2 bucket determined by the given type.
    /// </summary>
    /// <param name="type">The type defining the bucket (e.g. "Testset", "Problem", "Submission"). Case-insensitive.</param>
    /// <param name="id">The GUID identifier to be used as the file prefix.</param>
    /// <param name="fileExtension">The file extension (e.g. ".txt").</param>
    /// <param name="fileStream">The file content stream.</param>
    /// <param name="contentType">MIME content type.</param>
    /// <param name="cancellationToken"></param>
    Task UploadAsync(string type, Guid id, string fileExtension, Stream fileStream, string? contentType = null, CancellationToken cancellationToken = default);
    Task ReplaceIfExistsAsync(string type, Guid id, string fileExtension, Stream fileStream, string? contentType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a pre-signed URL for temporary access to an object determined by the given type and id.
    /// </summary>
    /// <param name="type">The type defining the bucket.</param>
    /// <param name="id">The GUID identifier of the file.</param>
    /// <param name="presignUrlMinute">Optional expiration time in minutes. Defaults to settings value if null.</param>
    /// <param name="cancellationToken"></param>
    Task<string?> GetPresignedUrlForViewAsync(string type, Guid id, int? presignUrlMinute = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a pre-signed URL for downloading an object determined by the given type and id (Attachment).
    /// </summary>
    /// <param name="type">The type defining the bucket.</param>
    /// <param name="id">The GUID identifier of the file.</param>
    /// <param name="presignUrlMinute">Optional expiration time in minutes. Defaults to settings value if null.</param>
    /// <param name="cancellationToken"></param>
    Task<string?> GetPresignedUrlForDownloadAsync(string type, Guid id, int? presignUrlMinute = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a public, unsigned URL (no expiration) for an object determined by the given type and id.
    /// Note: The bucket must be configured for public access for this URL to be accessible.
    /// </summary>
    /// <param name="type">The type defining the bucket.</param>
    /// <param name="id">The GUID identifier of the file.</param>
    /// <param name="cancellationToken"></param>
    Task<string?> GetPublicUrlAsync(string type, Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from the bucket determined by the given type and id.
    /// </summary>
    /// <param name="type">The type defining the bucket.</param>
    /// <param name="id">The GUID identifier of the file.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The full object key of the deleted file, or null if not found.</returns>
    Task<string?> DeleteAsync(string type, Guid id, CancellationToken cancellationToken = default);
}
