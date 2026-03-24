using Amazon.S3;
using Amazon.S3.Model;
using Application.Abstractions.Outbound.Services;
using Infrastructure.Configurations.FileStorage;
using Microsoft.Extensions.Options;

namespace Infrastructure.ExternalServices.FileStorage;

public class R2Service : IR2Service
{
    private const string TestsetBucket = "Testset";
    private const string ProblemBucket = "Problem";
    private const string SubmissionBucket = "Submission";

    private readonly R2Settings _settings;
    private readonly IAmazonS3 _s3Client;

    public R2Service(IOptions<R2Settings> settings)
    {
        _settings = settings.Value;

        var config = new AmazonS3Config
        {
            ServiceURL = _settings.ServiceUrl,
            ForcePathStyle = true // R2 requires path-style addressing
        };

        _s3Client = new AmazonS3Client(_settings.AccessKey, _settings.SecretKey, config);
    }

    // ─── Low-level methods ───────────────────────────────────────────

    public async Task UploadAsync(string bucketKey, string objectKey, Stream fileStream, string? contentType = null, CancellationToken cancellationToken = default)
    {
        var bucketName = ResolveBucket(bucketKey);

        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = fileStream,
            ContentType = contentType ?? "application/octet-stream",
            DisablePayloadSigning = true // R2 does not support payload signing
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<Stream> DownloadAsync(string bucketKey, string objectKey, CancellationToken cancellationToken = default)
    {
        var bucketName = ResolveBucket(bucketKey);

        var request = new GetObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey
        };

        var response = await _s3Client.GetObjectAsync(request, cancellationToken);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string bucketKey, string objectKey, CancellationToken cancellationToken = default)
    {
        var bucketName = ResolveBucket(bucketKey);

        var request = new DeleteObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey
        };

        await _s3Client.DeleteObjectAsync(request, cancellationToken);
    }

    public Task<string> GetPresignedUrlAsync(string bucketKey, string objectKey, TimeSpan expiresIn, CancellationToken cancellationToken = default)
    {
        var bucketName = ResolveBucket(bucketKey);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiresIn)
        };

        var url = _s3Client.GetPreSignedURL(request);
        return Task.FromResult(url);
    }

    // ─── GUID-based convenience methods ─────────────────────────────

    public async Task<Guid> UploadTestsetFileAsync(Stream fileStream, string fileExtension, string? contentType = null, CancellationToken cancellationToken = default)
    {
        var fileId = Guid.NewGuid();
        var objectKey = BuildObjectKey(fileId, fileExtension);
        await UploadAsync(TestsetBucket, objectKey, fileStream, contentType, cancellationToken);
        return fileId;
    }

    public async Task<Guid> UploadProblemFileAsync(Stream fileStream, string fileExtension, string? contentType = null, CancellationToken cancellationToken = default)
    {
        var fileId = Guid.NewGuid();
        var objectKey = BuildObjectKey(fileId, fileExtension);
        await UploadAsync(ProblemBucket, objectKey, fileStream, contentType, cancellationToken);
        return fileId;
    }

    public async Task<Guid> UploadSubmissionFileAsync(Stream fileStream, string fileExtension, string? contentType = null, CancellationToken cancellationToken = default)
    {
        var fileId = Guid.NewGuid();
        var objectKey = BuildObjectKey(fileId, fileExtension);
        await UploadAsync(SubmissionBucket, objectKey, fileStream, contentType, cancellationToken);
        return fileId;
    }

    public Task<Stream> DownloadTestsetFileAsync(Guid fileId, string fileExtension, CancellationToken cancellationToken = default)
        => DownloadAsync(TestsetBucket, BuildObjectKey(fileId, fileExtension), cancellationToken);

    public Task<Stream> DownloadProblemFileAsync(Guid fileId, string fileExtension, CancellationToken cancellationToken = default)
        => DownloadAsync(ProblemBucket, BuildObjectKey(fileId, fileExtension), cancellationToken);

    public Task<Stream> DownloadSubmissionFileAsync(Guid fileId, string fileExtension, CancellationToken cancellationToken = default)
        => DownloadAsync(SubmissionBucket, BuildObjectKey(fileId, fileExtension), cancellationToken);

    public Task DeleteTestsetFileAsync(Guid fileId, string fileExtension, CancellationToken cancellationToken = default)
        => DeleteAsync(TestsetBucket, BuildObjectKey(fileId, fileExtension), cancellationToken);

    public Task DeleteProblemFileAsync(Guid fileId, string fileExtension, CancellationToken cancellationToken = default)
        => DeleteAsync(ProblemBucket, BuildObjectKey(fileId, fileExtension), cancellationToken);

    public Task DeleteSubmissionFileAsync(Guid fileId, string fileExtension, CancellationToken cancellationToken = default)
        => DeleteAsync(SubmissionBucket, BuildObjectKey(fileId, fileExtension), cancellationToken);

    public Task<string> GetPresignedUrlAsync(string bucketKey, Guid fileId, string fileExtension, TimeSpan expiresIn, CancellationToken cancellationToken = default)
        => GetPresignedUrlAsync(bucketKey, BuildObjectKey(fileId, fileExtension), expiresIn, cancellationToken);

    // ─── Helpers ────────────────────────────────────────────────────

    private static string BuildObjectKey(Guid fileId, string fileExtension)
    {
        // Ensure extension starts with a dot
        if (!string.IsNullOrEmpty(fileExtension) && !fileExtension.StartsWith('.'))
            fileExtension = "." + fileExtension;

        return $"{fileId}{fileExtension}";
    }

    private string ResolveBucket(string bucketKey)
    {
        if (!_settings.Buckets.TryGetValue(bucketKey, out var bucketName))
            throw new ArgumentException($"Bucket key '{bucketKey}' is not configured. Available keys: {string.Join(", ", _settings.Buckets.Keys)}");

        return bucketName;
    }
}
