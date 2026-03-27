using Amazon.S3;
using Amazon.S3.Model;
using Application.Abstractions.Outbound.Services;
using Infrastructure.Configurations.FileStorage;
using Microsoft.Extensions.Options;

namespace Infrastructure.ExternalServices.FileStorage;

public sealed class R2Service : IR2Service
{
    private readonly R2Settings _settings;
    private readonly IAmazonS3 _s3Client;

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".json", ".cpp", ".py", ".cs",
        ".java", ".c", ".h", ".html", ".css", ".xml"
    };

    public R2Service(IOptions<R2Settings> settings)
    {
        _settings = settings.Value;

        var config = new AmazonS3Config
        {
            ServiceURL = _settings.ServiceUrl ,
            ForcePathStyle = true ,
            SignatureVersion = "4" ,
            AuthenticationRegion = "auto"
        };

        _s3Client = new AmazonS3Client(_settings.AccessKey , _settings.SecretKey , config);
    }

    // ─────────────────────────────────────────────
    // Upload
    // ─────────────────────────────────────────────

    public async Task UploadAsync(
        string type ,
        Guid id ,
        string fileExtension ,
        Stream fileStream ,
        string? contentType = null ,
        CancellationToken cancellationToken = default)
    {
        var objectKey = BuildObjectKey(id , fileExtension);

        await UploadObjectAsync(
            type ,
            objectKey ,
            fileStream ,
            contentType ,
            cancellationToken);
    }

    public async Task ReplaceIfExistsAsync(
        string type ,
        Guid id ,
        string fileExtension ,
        Stream fileStream ,
        string? contentType = null ,
        CancellationToken cancellationToken = default)
    {
        await DeleteAsync(type , id , cancellationToken);

        var objectKey = BuildObjectKey(id , fileExtension);

        await UploadObjectAsync(
            type ,
            objectKey ,
            fileStream ,
            contentType ,
            cancellationToken);
    }

    public async Task UploadObjectAsync(
        string type ,
        string objectKey ,
        Stream fileStream ,
        string? contentType = null ,
        CancellationToken cancellationToken = default)
    {
        var bucketName = ResolveBucket(type);

        var request = new PutObjectRequest
        {
            BucketName = bucketName ,
            Key = objectKey ,
            InputStream = fileStream ,
            ContentType = contentType ?? "application/octet-stream" ,
            DisablePayloadSigning = true
        };

        await _s3Client.PutObjectAsync(request , cancellationToken);
    }

    // ─────────────────────────────────────────────
    // Delete / List
    // ─────────────────────────────────────────────

    public async Task DeleteByPrefixAsync(
        string type ,
        string prefix ,
        CancellationToken cancellationToken = default)
    {
        var bucketName = ResolveBucket(type);

        string? continuationToken = null;

        do
        {
            var listRequest = new ListObjectsV2Request
            {
                BucketName = bucketName ,
                Prefix = prefix ,
                ContinuationToken = continuationToken
            };

            var listResponse = await _s3Client.ListObjectsV2Async(listRequest , cancellationToken);

            if ( listResponse.S3Objects.Count > 0 )
            {
                var deleteRequest = new DeleteObjectsRequest
                {
                    BucketName = bucketName ,
                    Objects = listResponse.S3Objects
                        .Select(x => new KeyVersion { Key = x.Key })
                        .ToList()
                };

                await _s3Client.DeleteObjectsAsync(deleteRequest , cancellationToken);
            }

            continuationToken = listResponse.IsTruncated
                ? listResponse.NextContinuationToken
                : null;

        } while ( !string.IsNullOrEmpty(continuationToken) );
    }

    public async Task<IReadOnlyList<string>> ListObjectKeysAsync(
        string type ,
        string prefix ,
        CancellationToken cancellationToken = default)
    {
        var bucketName = ResolveBucket(type);
        var result = new List<string>();

        string? continuationToken = null;

        do
        {
            var listRequest = new ListObjectsV2Request
            {
                BucketName = bucketName ,
                Prefix = prefix ,
                ContinuationToken = continuationToken
            };

            var listResponse = await _s3Client.ListObjectsV2Async(listRequest , cancellationToken);

            result.AddRange(listResponse.S3Objects.Select(x => x.Key));

            continuationToken = listResponse.IsTruncated
                ? listResponse.NextContinuationToken
                : null;

        } while ( !string.IsNullOrEmpty(continuationToken) );

        return result;
    }

    // ─────────────────────────────────────────────
    // URL
    // ─────────────────────────────────────────────

    public Task<string?> GetPresignedUrlForViewAsync(
        string type ,
        Guid id ,
        int? presignUrlMinute = null ,
        CancellationToken cancellationToken = default)
        => GeneratePresignedUrlAsync(type , id , presignUrlMinute , false , cancellationToken);

    public Task<string?> GetPresignedUrlForDownloadAsync(
        string type ,
        Guid id ,
        int? presignUrlMinute = null ,
        CancellationToken cancellationToken = default)
        => GeneratePresignedUrlAsync(type , id , presignUrlMinute , true , cancellationToken);

    private async Task<string?> GeneratePresignedUrlAsync(
        string type ,
        Guid id ,
        int? presignUrlMinute ,
        bool forDownload ,
        CancellationToken cancellationToken)
    {
        var bucketName = ResolveBucket(type);

        var listRequest = new ListObjectsV2Request
        {
            BucketName = bucketName ,
            Prefix = id.ToString() ,
            MaxKeys = 1
        };

        var listResponse = await _s3Client.ListObjectsV2Async(listRequest , cancellationToken);
        var fullKey = listResponse.S3Objects.FirstOrDefault()?.Key;

        if ( string.IsNullOrEmpty(fullKey) )
            return null;

        var fileName = Path.GetFileName(fullKey);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName ,
            Key = fullKey ,
            Verb = HttpVerb.GET ,
            Expires = DateTime.UtcNow.AddMinutes(
                presignUrlMinute ?? _settings.PresignedUrlExpirationMinutes)
        };

        request.ResponseHeaderOverrides = new ResponseHeaderOverrides();

        if ( forDownload )
        {
            request.ResponseHeaderOverrides.ContentDisposition =
                $"attachment; filename=\"{fileName}\"";
        }
        else
        {
            request.ResponseHeaderOverrides.ContentDisposition = "inline";

            // ✅ Fix encoding cho file text
            var ext = Path.GetExtension(fullKey);

            if ( !string.IsNullOrEmpty(ext) && TextExtensions.Contains(ext) )
            {
                request.ResponseHeaderOverrides.ContentType =
                    "text/plain; charset=utf-8";
            }
        }

        return _s3Client.GetPreSignedURL(request);
    }

    public async Task<string?> GetPublicUrlAsync(
        string type ,
        Guid id ,
        CancellationToken cancellationToken = default)
    {
        var bucketName = ResolveBucket(type);

        var listRequest = new ListObjectsV2Request
        {
            BucketName = bucketName ,
            Prefix = id.ToString() ,
            MaxKeys = 1
        };

        var listResponse = await _s3Client.ListObjectsV2Async(listRequest , cancellationToken);
        var fullKey = listResponse.S3Objects.FirstOrDefault()?.Key;

        if ( string.IsNullOrEmpty(fullKey) )
            return null;

        var bucketKey = string.IsNullOrEmpty(type)
            ? ""
            : char.ToUpper(type[0]) + type.Substring(1).ToLower();

        if ( _settings.PublicDomains != null &&
            _settings.PublicDomains.TryGetValue(bucketKey , out var publicDomain) &&
            !string.IsNullOrWhiteSpace(publicDomain) )
        {
            return $"{publicDomain.TrimEnd('/')}/{fullKey}";
        }

        return $"{_settings.ServiceUrl.TrimEnd('/')}/{bucketName}/{fullKey}";
    }

    public async Task<string?> DeleteAsync(
        string type ,
        Guid id ,
        CancellationToken cancellationToken = default)
    {
        var bucketName = ResolveBucket(type);

        var listRequest = new ListObjectsV2Request
        {
            BucketName = bucketName ,
            Prefix = id.ToString() ,
            MaxKeys = 1
        };

        var listResponse = await _s3Client.ListObjectsV2Async(listRequest , cancellationToken);
        var fullKey = listResponse.S3Objects.FirstOrDefault()?.Key;

        if ( string.IsNullOrEmpty(fullKey) )
            return null;

        var request = new DeleteObjectRequest
        {
            BucketName = bucketName ,
            Key = fullKey
        };

        await _s3Client.DeleteObjectAsync(request , cancellationToken);
        return fullKey;
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    private static string BuildObjectKey(Guid fileId , string fileExtension)
    {
        if ( !string.IsNullOrEmpty(fileExtension) && !fileExtension.StartsWith('.') )
            fileExtension = "." + fileExtension;

        return $"{fileId}{fileExtension}";
    }

    private string ResolveBucket(string type)
    {
        var bucketKey = string.IsNullOrEmpty(type)
            ? ""
            : char.ToUpper(type[0]) + type.Substring(1).ToLower();

        if ( !_settings.Buckets.TryGetValue(bucketKey , out var bucketName) )
        {
            throw new ArgumentException(
                $"Bucket type '{type}' is not configured. Available types: {string.Join(", " , _settings.Buckets.Keys)}");
        }

        return bucketName;
    }
}