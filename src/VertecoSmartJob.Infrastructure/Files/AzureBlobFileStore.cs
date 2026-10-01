using System.Security.Cryptography;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using VertecoSmartJob.Application.Files;

namespace VertecoSmartJob.Infrastructure.Files;

// Handles private Azure Blob Storage for evidence files
public sealed class AzureBlobFileStore(BlobContainerClient container) : IFieldFileStore
{
    private const long MaxFileSize = 10 * 1024 * 1024;

    private static readonly Dictionary<string, string> ContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".png"] = "image/png",
            [".pdf"] = "application/pdf"
        };

    public async Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken ct = default)
    {
        extension = NormalizeExtension(extension);

        if (!ContentTypes.TryGetValue(extension, out var contentType))
            throw new ArgumentException(
                "Only JPEG, PNG and PDF files are accepted.",
                nameof(extension));

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);

        if (buffer.Length is <= 0 or > MaxFileSize)
            throw new ArgumentException(
                "File size must be between 1 byte and 10 MB.",
                nameof(content));

        var hash = Convert.ToHexString(
            SHA256.HashData(buffer.ToArray()));

        var name = $"{Guid.NewGuid():N}{extension}";

        buffer.Position = 0;

        await container.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: ct);

        await container.GetBlobClient(name).UploadAsync(
            buffer,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            },
            ct);

        return $"blob:{name}:sha256:{hash}";
    }

    public async Task DeleteAsync(
        string reference,
        CancellationToken ct = default)
    {
        // Removes a file if saving the evidence record fails
        var parts = reference.Split(':');

        if (parts.Length != 4 ||
            parts[0] != "blob" ||
            parts[2] != "sha256" ||
            !Guid.TryParseExact(
                Path.GetFileNameWithoutExtension(parts[1]),
                "N",
                out _))
        {
            throw new ArgumentException(
                "Invalid blob reference.",
                nameof(reference));
        }

        await container
            .GetBlobClient(parts[1])
            .DeleteIfExistsAsync(cancellationToken: ct);
    }

    public async Task<FieldUploadInstruction> CreateUploadInstructionAsync(
        string extension,
        CancellationToken ct = default)
    {
        extension = NormalizeExtension(extension);

        if (!ContentTypes.ContainsKey(extension))
            throw new ArgumentException(
                "Only JPEG, PNG and PDF files are accepted.",
                nameof(extension));

        await container.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: ct);

        var blobName = $"{Guid.NewGuid():N}{extension}";
        var blob = container.GetBlobClient(blobName);

        // The mobile app only gets permission to upload this one file
        if (!blob.CanGenerateSasUri)
            throw new InvalidOperationException(
                "Blob storage cannot generate an upload link with the current credentials.");

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(10);

        var sas = new BlobSasBuilder
        {
            BlobContainerName = container.Name,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = expiresAt
        };

        sas.SetPermissions(
            BlobSasPermissions.Create |
            BlobSasPermissions.Write);

        var uploadUrl = blob.GenerateSasUri(sas).ToString();

        return new FieldUploadInstruction
        {
            UploadToken = blobName,
            UploadUrl = uploadUrl,
            ExpiresAtUtc = expiresAt
        };
    }

    public async Task<string> ConfirmUploadAsync(
        string uploadToken,
        string expectedSha256,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(uploadToken))
            throw new ArgumentException(
                "Upload token is required.",
                nameof(uploadToken));

        if (string.IsNullOrWhiteSpace(expectedSha256))
            throw new ArgumentException(
                "SHA-256 checksum is required.",
                nameof(expectedSha256));

        // Stops a token from pointing outside the evidence container
        if (Path.GetFileName(uploadToken) != uploadToken)
            throw new ArgumentException(
                "Invalid upload token.",
                nameof(uploadToken));

        var extension = NormalizeExtension(
            Path.GetExtension(uploadToken));

        if (!ContentTypes.TryGetValue(extension, out var expectedContentType))
            throw new ArgumentException(
                "Only JPEG, PNG and PDF files are accepted.",
                nameof(uploadToken));

        if (!Guid.TryParseExact(
                Path.GetFileNameWithoutExtension(uploadToken),
                "N",
                out _))
        {
            throw new ArgumentException(
                "Invalid upload token.",
                nameof(uploadToken));
        }

        var blob = container.GetBlobClient(uploadToken);

        if (!await blob.ExistsAsync(ct))
            throw new InvalidOperationException(
                "The uploaded file was not found.");

        var properties = await blob.GetPropertiesAsync(
            cancellationToken: ct);

        if (properties.Value.ContentLength is <= 0 or > MaxFileSize)
            throw new InvalidOperationException(
                "File size must be between 1 byte and 10 MB.");

        // Checks that the uploaded file type matches the extension
        if (!string.IsNullOrWhiteSpace(properties.Value.ContentType) &&
            !string.Equals(
                properties.Value.ContentType,
                expectedContentType,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The uploaded file type does not match its extension.");
        }

        await using var content = await blob.OpenReadAsync(
            cancellationToken: ct);

        var actualHash = Convert.ToHexString(
            await SHA256.HashDataAsync(content, ct));

        var suppliedHash = expectedSha256
            .Trim()
            .Replace("-", string.Empty)
            .ToUpperInvariant();

        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(actualHash),
                ParseHash(suppliedHash)))
        {
            throw new InvalidOperationException(
                "The uploaded file checksum does not match.");
        }

        return $"blob:{uploadToken}:sha256:{actualHash}";
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return string.Empty;

        extension = extension.Trim();

        if (!extension.StartsWith('.'))
            extension = $".{extension}";

        return extension.ToLowerInvariant();
    }

    private static byte[] ParseHash(string hash)
    {
        try
        {
            var bytes = Convert.FromHexString(hash);

            if (bytes.Length != 32)
                throw new FormatException();

            return bytes;
        }
        catch (FormatException)
        {
            throw new ArgumentException(
                "SHA-256 checksum must be a valid 64-character hexadecimal value.",
                nameof(hash));
        }
    }
}