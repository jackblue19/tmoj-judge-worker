using Amazon.S3;
using Amazon.S3.Model;
using Application.Abstractions.Outbound.Services;
using Infrastructure.Configurations.FileStorage;
using Microsoft.Extensions.Options;

namespace Infrastructure.ExternalServices.FileStorage;

public class R2Service : IR2Service
{
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

    public async Task UploadAsync(string type, Guid id, string fileExtension, Stream fileStream, string? contentType = null, CancellationToken cancellationToken = default)
    {
        var bucketName = ResolveBucket(type);
        var objectKey = BuildObjectKey(id, fileExtension);

        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = fileStream,
            ContentType = contentType ?? "application/octet-stream",
            DisablePayloadSigning = true
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<string?> GetPresignedUrlAsync(string type, Guid id, TimeSpan? expiresIn = null, CancellationToken cancellationToken = default)
    {
        var bucketName = ResolveBucket(type);

        var listRequest = new ListObjectsV2Request
        {
            BucketName = bucketName,
            Prefix = id.ToString(),
            MaxKeys = 1
        };

        var listResponse = await _s3Client.ListObjectsV2Async(listRequest, cancellationToken);
        var fullKey = listResponse.S3Objects.FirstOrDefault()?.Key;

        if (string.IsNullOrEmpty(fullKey))
            return null;

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = fullKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiresIn ?? TimeSpan.FromMinutes(3))
        };

        return _s3Client.GetPreSignedURL(request);
    }

    public async Task<string?> DeleteAsync(string type, Guid id, CancellationToken cancellationToken = default)
    {
        var bucketName = ResolveBucket(type);

        var listRequest = new ListObjectsV2Request
        {
            BucketName = bucketName,
            Prefix = id.ToString(),
            MaxKeys = 1
        };

        var listResponse = await _s3Client.ListObjectsV2Async(listRequest, cancellationToken);
        var fullKey = listResponse.S3Objects.FirstOrDefault()?.Key;

        if (!string.IsNullOrEmpty(fullKey))
        {
            var request = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = fullKey
            };

            await _s3Client.DeleteObjectAsync(request, cancellationToken);
            return fullKey;
        }

        return null;
    }

    // ─── Helpers ────────────────────────────────────────────────────

    private static string BuildObjectKey(Guid fileId, string fileExtension)
    {
        if (!string.IsNullOrEmpty(fileExtension) && !fileExtension.StartsWith('.'))
            fileExtension = "." + fileExtension;

        return $"{fileId}{fileExtension}";
    }

    private string ResolveBucket(string type)
    {
        var bucketKey = string.IsNullOrEmpty(type) ? "" : char.ToUpper(type[0]) + type.Substring(1).ToLower();

        if (!_settings.Buckets.TryGetValue(bucketKey, out var bucketName))
            throw new ArgumentException($"Bucket type '{type}' is not configured. Available types are based on: {string.Join(", ", _settings.Buckets.Keys)}");

        return bucketName;
    }
}
