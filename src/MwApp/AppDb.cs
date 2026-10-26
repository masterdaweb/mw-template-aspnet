using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MwApp;

/// <summary>
/// The app's PostgreSQL. Changing the model? Add a migration
/// (<c>dotnet ef migrations add Name --project src/MwApp</c>) and push: it
/// runs once per deploy, before the new version takes traffic.
/// </summary>
public sealed class AppDb(DbContextOptions<AppDb> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<StoredFile> Files => Set<StoredFile>();

    /// <summary>
    /// The keys login cookies and form tokens are encrypted with, shared by
    /// every instance and kept across deploys (ASP.NET Data Protection).
    /// </summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
}

/// <summary>A file in the bucket, described here.</summary>
public sealed class StoredFile
{
    public long Id { get; set; }
    public required string ObjectKey { get; set; }
    public required string Name { get; set; }
    public long Size { get; set; }
    public required string ContentType { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
