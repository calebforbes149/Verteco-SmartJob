using System.Security.Cryptography;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using VertecoSmartJob.Application.Files;

namespace VertecoSmartJob.Infrastructure.Files;

/// <summary>
/// Private Blob Storage adapter for the shared evidence contract. I think Isaac part will be to build the
/// request/upload/confirm workflow on this interface without changing the job service.
/// </summary>
public sealed class AzureBlobFileStore(BlobContainerClient container) : IFieldFileStore
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg", [".png"] = "image/png", [".pdf"] = "application/pdf"
    };

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken ct = default)
    {
        if (!ContentTypes.TryGetValue(extension, out var contentType))
            throw new ArgumentException("Only JPEG, PNG and PDF files are accepted.", nameof(extension));

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        if (buffer.Length is <= 0 or > 10 * 1024 * 1024)
            throw new ArgumentException("File size must be between 1 byte and 10 MB.", nameof(content));

        var hash = Convert.ToHexString(SHA256.HashData(buffer.ToArray()));
        var name = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        buffer.Position = 0;
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        await container.GetBlobClient(name).UploadAsync(buffer,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } }, ct);
        return $"blob:{name}:sha256:{hash}";
    }

    public async Task DeleteAsync(string reference, CancellationToken ct = default)
    {
        // Used only to roll back a failed metadata write; retained job records are never deleted.
        var parts = reference.Split(':');
        if (parts.Length != 4 || parts[0] != "blob" || parts[2] != "sha256" ||
            !Guid.TryParseExact(Path.GetFileNameWithoutExtension(parts[1]), "N", out _))
            throw new ArgumentException("Invalid blob reference.", nameof(reference));
        await container.GetBlobClient(parts[1]).DeleteIfExistsAsync(cancellationToken: ct);
    }
}
