using System.Security.Cryptography;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using VertecoSmartJob.Application.Files;

namespace VertecoSmartJob.Infrastructure.Files;

// Handles private Azure Blob Storage for evidence files
public sealed class AzureBlobFileStore(
    BlobContainerClient container,
    BlobServiceClient serviceClient) : IFieldFileStore
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

        if (!MatchesFileSignature(buffer.ToArray(), extension))
            throw new ArgumentException(
                "The file contents do not match its extension.",
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

    public async Task<FieldStoredFile?> ReadAsync(
        string reference,
        CancellationToken ct = default)
    {
        var parts = reference.Split(':');
        if (parts.Length != 4 || parts[0] != "blob" || parts[2] != "sha256" ||
            parts[3].Length != 64 || !parts[3].All(Uri.IsHexDigit) ||
            !Guid.TryParseExact(Path.GetFileNameWithoutExtension(parts[1]), "N", out _) ||
            !ContentTypes.TryGetValue(Path.GetExtension(parts[1]), out var contentType))
            throw new ArgumentException("Invalid blob reference.", nameof(reference));

        var blob = container.GetBlobClient(parts[1]);
        if (!await blob.ExistsAsync(ct))
            return null;

        return new FieldStoredFile(await blob.OpenReadAsync(cancellationToken: ct), contentType);
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

        // Shared-key connections can sign a service SAS. Azure identity connections
        // need a user delegation key from the storage account instead.
        string uploadUrl;
        if (blob.CanGenerateSasUri)
        {
            uploadUrl = blob.GenerateSasUri(sas).ToString();
        }
        else
        {
            var delegationKey = await serviceClient.GetUserDelegationKeyAsync(
                DateTimeOffset.UtcNow.AddMinutes(-5),
                expiresAt,
                ct);
            uploadUrl = blob.GenerateUserDelegationSasUri(
                sas,
                delegationKey.Value).ToString();
        }

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
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        if (buffer.Length is <= 0 or > MaxFileSize)
            throw new InvalidOperationException(
                "File size must be between 1 byte and 10 MB.");

        var bytes = buffer.ToArray();
        if (!MatchesFileSignature(bytes, extension))
            throw new InvalidOperationException(
                "The uploaded file contents do not match its extension.");

        var actualHash = Convert.ToHexString(SHA256.HashData(bytes));

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

    private static bool MatchesFileSignature(ReadOnlySpan<byte> content, string extension) =>
        extension switch
        {
            ".jpg" => content.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }),
            ".png" => content.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".pdf" => content.StartsWith("%PDF-"u8),
            _ => false
        };

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
