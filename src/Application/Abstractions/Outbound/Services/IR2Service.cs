namespace Application.Abstractions.Outbound.Services;

public interface IR2Service
{
    // ─── Low-level methods ───────────────────────────────────────────

    /// <summary>
    /// Upload a file stream to a Cloudflare R2 bucket.
    /// </summary>
    /// <param name="bucketKey">Logical bucket key defined in R2Settings.Buckets (e.g. "Problem", "Testset").</param>
    /// <param name="objectKey">The object key (path) inside the bucket.</param>
    /// <param name="fileStream">The file content stream.</param>
    /// <param name="contentType">MIME content type.</param>
    /// <param name="cancellationToken"></param>
    Task UploadAsync(string bucketKey, string objectKey, Stream fileStream, string? contentType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a file from R2 as a stream.
    /// </summary>
    Task<Stream> DownloadAsync(string bucketKey, string objectKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an object from an R2 bucket.
    /// </summary>
    Task DeleteAsync(string bucketKey, string objectKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a pre-signed URL for temporary access to an object.
    /// </summary>
    Task<string> GetPresignedUrlAsync(string bucketKey, string objectKey, TimeSpan expiresIn, CancellationToken cancellationToken = default);

    // ─── GUID-based convenience methods ─────────────────────────────

    /// <summary>
    /// Upload a testset file (input or output) to R2.
    /// A new GUID is generated as the object key.
    /// Bucket key: "Testset".
    /// </summary>
    /// <returns>The GUID used as the object key.</returns>
    Task<Guid> UploadTestsetFileAsync(Stream fileStream, string fileExtension, string? contentType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload a problem-related file (description, attachment, etc.) to R2.
    /// A new GUID is generated as the object key.
    /// Bucket key: "Problem".
    /// </summary>
    /// <returns>The GUID used as the object key.</returns>
    Task<Guid> UploadProblemFileAsync(Stream fileStream, string fileExtension, string? contentType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload a submission source code or artifact to R2.
    /// A new GUID is generated as the object key.
    /// Bucket key: "Submission".
    /// </summary>
    /// <returns>The GUID used as the object key.</returns>
    Task<Guid> UploadSubmissionFileAsync(Stream fileStream, string fileExtension, string? contentType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a testset file from R2 by its GUID.
    /// </summary>
    Task<Stream> DownloadTestsetFileAsync(Guid fileId, string fileExtension, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a problem file from R2 by its GUID.
    /// </summary>
    Task<Stream> DownloadProblemFileAsync(Guid fileId, string fileExtension, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a submission file from R2 by its GUID.
    /// </summary>
    Task<Stream> DownloadSubmissionFileAsync(Guid fileId, string fileExtension, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a testset file from R2 by its GUID.
    /// </summary>
    Task DeleteTestsetFileAsync(Guid fileId, string fileExtension, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a problem file from R2 by its GUID.
    /// </summary>
    Task DeleteProblemFileAsync(Guid fileId, string fileExtension, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a submission file from R2 by its GUID.
    /// </summary>
    Task DeleteSubmissionFileAsync(Guid fileId, string fileExtension, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a pre-signed URL for a file by bucket key and GUID.
    /// </summary>
    Task<string> GetPresignedUrlAsync(string bucketKey, Guid fileId, string fileExtension, TimeSpan expiresIn, CancellationToken cancellationToken = default);
}
