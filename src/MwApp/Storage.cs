using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace MwApp;

/// <summary>
/// The private bucket bound to this service (prefix MW_STORAGE_). Its files
/// are never public: each link is signed and expires.
/// </summary>
public sealed class Storage
{
    readonly AmazonS3Client s3;
    // Links are signed for the endpoint's own scheme: the SDK would make them
    // https even for an http endpoint (a local MinIO, say).
    readonly Protocol protocol;

    public string Bucket { get; }

    public Storage(IConfiguration config)
    {
        Bucket = config["MW_STORAGE_BUCKET"] ?? "";
        var endpoint = config["MW_STORAGE_ENDPOINT"] ?? "https://s3.amazonaws.com";
        protocol = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? Protocol.HTTP : Protocol.HTTPS;
        s3 = new AmazonS3Client(
            new BasicAWSCredentials(config["MW_STORAGE_ACCESS_KEY_ID"] ?? "", config["MW_STORAGE_SECRET_ACCESS_KEY"] ?? ""),
            new AmazonS3Config
            {
                ServiceURL = endpoint,
                AuthenticationRegion = config["MW_STORAGE_REGION"] ?? "auto",
                ForcePathStyle = config["MW_STORAGE_FORCE_PATH_STYLE"] == "true",
                // Checksums only where S3 requires them, as most S3-compatible stores expect.
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
            });
    }

    public async Task<string> Check(CancellationToken ct)
    {
        if (Bucket == "") throw new InvalidOperationException("MW_STORAGE_BUCKET is empty: connect a bucket to this service");
        await s3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = Bucket, MaxKeys = 1 }, ct);
        return Bucket;
    }

    public Task Put(string key, Stream body, string contentType, CancellationToken ct) =>
        s3.PutObjectAsync(new PutObjectRequest { BucketName = Bucket, Key = key, InputStream = body, ContentType = contentType }, ct);

    /// <summary>A link to download the file, good for 15 minutes.</summary>
    public Task<string> SignedLink(string key, string name) =>
        s3.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = Bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Protocol = protocol,
            Expires = DateTime.UtcNow.AddMinutes(15),
            ResponseHeaderOverrides = { ContentDisposition = $"attachment; filename=\"{name.Replace("\"", "")}\"" },
        });
}
