using Amazon.S3;
using Amazon.S3.Model;
using LzDetailingStore.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LzDetailingStore.Infrastructure.Services;

public sealed class S3Service : IS3Service, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;
    private readonly string _cdnBaseUrl;
    private readonly ILogger<S3Service> _logger;

    public S3Service(IConfiguration configuration, ILogger<S3Service> logger)
    {
        _logger = logger;

        var accessKey = configuration["Storage:AccessKey"] ?? string.Empty;
        var secretKey = configuration["Storage:SecretKey"] ?? string.Empty;
        var serviceUrl = configuration["Storage:ServiceUrl"] ?? "https://s3.fleekware.com";
        _bucketName = configuration["Storage:BucketName"] ?? "public";
        _cdnBaseUrl = configuration["Storage:CdnBaseUrl"] ?? "https://cdn.fleekware.com/buckets/public";

        var config = new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = true
        };

        _client = new AmazonS3Client(accessKey, secretKey, config);
    }

    public async Task<string> UploadAsync(Stream stream, string key, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType
        };

        try
        {
            await _client.PutObjectAsync(request, ct);
            var cdnUrl = $"{_cdnBaseUrl.TrimEnd('/')}/{key}";
            _logger.LogInformation("S3 Upload: {Key} -> {Url}", key, cdnUrl);
            return cdnUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "S3 Upload failed for key: {Key}", key);
            throw;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };

        try
        {
            await _client.DeleteObjectAsync(request, ct);
            _logger.LogInformation("S3 Delete: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "S3 Delete failed for key: {Key}", key);
            throw;
        }
    }

    public void Dispose() => _client.Dispose();
}
