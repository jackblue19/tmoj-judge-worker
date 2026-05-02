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
        ".java", ".c", ".h", ".html", ".css", ".xml",
        ".inp", ".out"
    };

    public R2Service(IOptions<R2Settings> settings)
    {
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));

        if ( string.IsNullOrWhiteSpace(_settings.ServiceUrl) )
            throw new InvalidOperationException("R2Settings.ServiceUrl is required.");

        if ( string.IsNullOrWhiteSpace(_settings.AccessKey) )
            throw new InvalidOperationException("R2Settings.AccessKey is required.");

        if ( string.IsNullOrWhiteSpace(_settings.SecretKey) )
            throw new InvalidOperationException("R2Settings.SecretKey is required.");

        if ( _settings.Buckets is null || _settings.Buckets.Count == 0 )
            throw new InvalidOperationException("R2Settings.Buckets is required.");

        var config = new AmazonS3Config
        {
            ServiceURL = _settings.ServiceUrl ,
            ForcePathStyle = true ,
            AuthenticationRegion = "auto"
        };

        _s3Client = new AmazonS3Client(
            _settings.AccessKey ,
            _settings.SecretKey ,
            config);
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
        if ( string.IsNullOrWhiteSpace(objectKey) )
            throw new ArgumentException("Object key is required." , nameof(objectKey));

        if ( fileStream is null )
            throw new ArgumentNullException(nameof(fileStream));

        if ( fileStream.CanSeek )
            fileStream.Position = 0;

        var bucketName = ResolveBucket(type);

        var request = new PutObjectRequest
        {
            BucketName = bucketName ,
            Key = NormalizeObjectKey(objectKey) ,
            InputStream = fileStream ,
            ContentType = contentType ?? "application/octet-stream" ,

            // Cloudflare R2/S3-compatible storage thường ổn hơn khi tắt payload signing.
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
        if ( string.IsNullOrWhiteSpace(prefix) )
            throw new ArgumentException("Prefix is required." , nameof(prefix));

        var bucketName = ResolveBucket(type);
        var normalizedPrefix = NormalizePrefix(prefix);

        string? continuationToken = null;

        do
        {
            var listRequest = new ListObjectsV2Request
            {
                BucketName = bucketName ,
                Prefix = normalizedPrefix ,
                ContinuationToken = continuationToken
            };

            var listResponse = await _s3Client.ListObjectsV2Async(listRequest , cancellationToken);
            var objects = listResponse.S3Objects ?? new List<S3Object>();

            if ( objects.Count > 0 )
            {
                var deleteRequest = new DeleteObjectsRequest
                {
                    BucketName = bucketName ,
                    Objects = objects
                        .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                        .Select(x => new KeyVersion { Key = x.Key })
                        .ToList()
                };

                if ( deleteRequest.Objects.Count > 0 )
                    await _s3Client.DeleteObjectsAsync(deleteRequest , cancellationToken);
            }

            continuationToken = listResponse.IsTruncated == true
                ? listResponse.NextContinuationToken
                : null;

        } while ( !string.IsNullOrWhiteSpace(continuationToken) );
    }

    public async Task<IReadOnlyList<string>> ListObjectKeysAsync(
        string type ,
        string prefix ,
        CancellationToken cancellationToken = default)
    {
        var bucketName = ResolveBucket(type);
        var normalizedPrefix = NormalizePrefix(prefix);

        var result = new List<string>();
        string? continuationToken = null;

        do
        {
            var listRequest = new ListObjectsV2Request
            {
                BucketName = bucketName ,
                Prefix = normalizedPrefix ,
                ContinuationToken = continuationToken
            };

            var listResponse = await _s3Client.ListObjectsV2Async(listRequest , cancellationToken);
            var objects = listResponse.S3Objects ?? new List<S3Object>();

            result.AddRange(
                objects
                    .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                    .Select(x => x.Key));

            continuationToken = listResponse.IsTruncated == true
                ? listResponse.NextContinuationToken
                : null;

        } while ( !string.IsNullOrWhiteSpace(continuationToken) );

        return result;
    }

    public async Task<string?> DeleteAsync(
        string type ,
        Guid id ,
        CancellationToken cancellationToken = default)
    {
        var bucketName = ResolveBucket(type);

        var fullKey = await FindFirstObjectKeyByIdAsync(
            bucketName ,
            id ,
            cancellationToken);

        if ( string.IsNullOrWhiteSpace(fullKey) )
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
    // URL
    // ─────────────────────────────────────────────

    public Task<string?> GetPresignedUrlForViewAsync(
        string type ,
        Guid id ,
        int? presignUrlMinute = null ,
        CancellationToken cancellationToken = default)
        => GeneratePresignedUrlAsync(
            type ,
            id ,
            presignUrlMinute ,
            forDownload: false ,
            cancellationToken);

    public Task<string?> GetPresignedUrlForDownloadAsync(
        string type ,
        Guid id ,
        int? presignUrlMinute = null ,
        CancellationToken cancellationToken = default)
        => GeneratePresignedUrlAsync(
            type ,
            id ,
            presignUrlMinute ,
            forDownload: true ,
            cancellationToken);

    private async Task<string?> GeneratePresignedUrlAsync(
        string type ,
        Guid id ,
        int? presignUrlMinute ,
        bool forDownload ,
        CancellationToken cancellationToken)
    {
        var bucketName = ResolveBucket(type);

        var fullKey = await FindFirstObjectKeyByIdAsync(
            bucketName ,
            id ,
            cancellationToken);

        if ( string.IsNullOrWhiteSpace(fullKey) )
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

            var ext = Path.GetExtension(fullKey);

            if ( !string.IsNullOrWhiteSpace(ext) && TextExtensions.Contains(ext) )
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

        var fullKey = await FindFirstObjectKeyByIdAsync(
            bucketName ,
            id ,
            cancellationToken);

        if ( string.IsNullOrWhiteSpace(fullKey) )
            return null;

        var bucketKey = BuildBucketKey(type);

        if ( _settings.PublicDomains != null &&
            _settings.PublicDomains.TryGetValue(bucketKey , out var publicDomain) &&
            !string.IsNullOrWhiteSpace(publicDomain) )
        {
            return $"{publicDomain.TrimEnd('/')}/{fullKey}";
        }

        return $"{_settings.ServiceUrl.TrimEnd('/')}/{bucketName}/{fullKey}";
    }

    public Task<string> GetPresignedObjectUrlForViewAsync(
        string bucketType ,
        string objectKey ,
        TimeSpan? expiresIn = null ,
        CancellationToken cancellationToken = default)
    {
        var bucketName = ResolveBucket(bucketType);
        var normalizedObjectKey = NormalizeObjectKey(objectKey);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName ,
            Key = normalizedObjectKey ,
            Verb = HttpVerb.GET ,
            Expires = DateTime.UtcNow.Add(
                expiresIn ?? TimeSpan.FromMinutes(_settings.PresignedUrlExpirationMinutes))
        };

        request.ResponseHeaderOverrides = new ResponseHeaderOverrides
        {
            ContentDisposition = "inline"
        };

        var ext = Path.GetExtension(normalizedObjectKey);

        if ( !string.IsNullOrWhiteSpace(ext) && TextExtensions.Contains(ext) )
        {
            request.ResponseHeaderOverrides.ContentType =
                "text/plain; charset=utf-8";
        }

        return Task.FromResult(_s3Client.GetPreSignedURL(request));
    }

    public Task<string> GetPresignedObjectUrlForDownloadAsync(
        string bucketType ,
        string objectKey ,
        string fileName ,
        TimeSpan? expiresIn = null ,
        CancellationToken cancellationToken = default)
    {
        var bucketName = ResolveBucket(bucketType);
        var normalizedObjectKey = NormalizeObjectKey(objectKey);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName ,
            Key = normalizedObjectKey ,
            Verb = HttpVerb.GET ,
            Expires = DateTime.UtcNow.Add(
                expiresIn ?? TimeSpan.FromMinutes(_settings.PresignedUrlExpirationMinutes))
        };

        request.ResponseHeaderOverrides = new ResponseHeaderOverrides
        {
            ContentDisposition = $"attachment; filename=\"{fileName}\""
        };

        return Task.FromResult(_s3Client.GetPreSignedURL(request));
    }

    public async Task<string> GetObjectTextAsync(
        string bucketType ,
        string objectKey ,
        CancellationToken cancellationToken = default)
    {
        if ( string.IsNullOrWhiteSpace(objectKey) )
            throw new ArgumentException("Object key is required." , nameof(objectKey));

        var bucketName = ResolveBucket(bucketType);

        var request = new GetObjectRequest
        {
            BucketName = bucketName ,
            Key = NormalizeObjectKey(objectKey)
        };

        using var response = await _s3Client.GetObjectAsync(request , cancellationToken);
        await using var responseStream = response.ResponseStream;
        using var reader = new StreamReader(responseStream , System.Text.Encoding.UTF8);

        return await reader.ReadToEndAsync(cancellationToken);
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    private async Task<string?> FindFirstObjectKeyByIdAsync(
        string bucketName ,
        Guid id ,
        CancellationToken cancellationToken)
    {
        var listRequest = new ListObjectsV2Request
        {
            BucketName = bucketName ,
            Prefix = id.ToString("D") ,
            MaxKeys = 1
        };

        var listResponse = await _s3Client.ListObjectsV2Async(
            listRequest ,
            cancellationToken);

        return listResponse.S3Objects?
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .Select(x => x.Key)
            .FirstOrDefault();
    }

    private static string BuildObjectKey(Guid fileId , string fileExtension)
    {
        if ( !string.IsNullOrWhiteSpace(fileExtension) &&
            !fileExtension.StartsWith('.') )
        {
            fileExtension = "." + fileExtension;
        }

        return $"{fileId:D}{fileExtension}";
    }

    private string ResolveBucket(string type)
    {
        var bucketKey = BuildBucketKey(type);

        if ( _settings.Buckets is null ||
            !_settings.Buckets.TryGetValue(bucketKey , out var bucketName) ||
            string.IsNullOrWhiteSpace(bucketName) )
        {
            var availableTypes = _settings.Buckets is null
                ? string.Empty
                : string.Join(", " , _settings.Buckets.Keys);

            throw new ArgumentException(
                $"Bucket type '{type}' is not configured. Available types: {availableTypes}");
        }

        return bucketName;
    }

    private static string BuildBucketKey(string type)
    {
        if ( string.IsNullOrWhiteSpace(type) )
            return string.Empty;

        type = type.Trim();

        return char.ToUpperInvariant(type[0]) +
               type[1..].ToLowerInvariant();
    }

    private static string NormalizeObjectKey(string objectKey)
    {
        if ( string.IsNullOrWhiteSpace(objectKey) )
            throw new ArgumentException("Object key is required." , nameof(objectKey));

        return objectKey
            .Replace('\\' , '/')
            .Trim()
            .TrimStart('/');
    }

    private static string NormalizePrefix(string prefix)
    {
        if ( string.IsNullOrWhiteSpace(prefix) )
            return string.Empty;

        return prefix
            .Replace('\\' , '/')
            .Trim()
            .TrimStart('/');
    }
}