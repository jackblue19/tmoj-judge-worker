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

    /// <summary>
    /// Generates a pre-signed URL for temporary access to an object determined by the given type and id.
    /// </summary>
    /// <param name="type">The type defining the bucket.</param>
    /// <param name="id">The GUID identifier of the file.</param>
    /// <param name="expiresIn">Optional expiration time. Defaults to 3 minutes internally if null.</param>
    /// <param name="cancellationToken"></param>
    Task<string?> GetPresignedUrlAsync(string type, Guid id, TimeSpan? expiresIn = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from the bucket determined by the given type and id.
    /// </summary>
    /// <param name="type">The type defining the bucket.</param>
    /// <param name="id">The GUID identifier of the file.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The full object key of the deleted file, or null if not found.</returns>
    Task<string?> DeleteAsync(string type, Guid id, CancellationToken cancellationToken = default);
}
