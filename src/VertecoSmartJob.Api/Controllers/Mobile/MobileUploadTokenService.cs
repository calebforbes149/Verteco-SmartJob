using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace VertecoSmartJob.Api.Controllers.Mobile;

public enum MobileUploadKind { Evidence, Signature }

// A signed upload ticket binds a blob to one technician, job and use.
public sealed class MobileUploadTokenService
{
    private readonly byte[] signingKey;

    public MobileUploadTokenService(IConfiguration configuration)
    {
        var key = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Jwt:SigningKey is required for mobile uploads.");
        signingKey = Encoding.UTF8.GetBytes(key);
    }

    public string Issue(string blobName, Guid jobId, Guid technicianId,
        MobileUploadKind kind, DateTimeOffset expiresAtUtc)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(
            new UploadTicket(blobName, jobId, technicianId, kind, expiresAtUtc));
        var signature = HMACSHA256.HashData(signingKey, payload);
        return $"{WebEncoders.Base64UrlEncode(payload)}.{WebEncoders.Base64UrlEncode(signature)}";
    }

    public bool TryRead(string? token, Guid jobId, Guid technicianId,
        MobileUploadKind kind, out string blobName)
    {
        blobName = string.Empty;
        if (string.IsNullOrWhiteSpace(token) || token.Length > 2048)
            return false;

        var parts = token.Split('.');
        if (parts.Length != 2)
            return false;

        try
        {
            var payload = WebEncoders.Base64UrlDecode(parts[0]);
            var signature = WebEncoders.Base64UrlDecode(parts[1]);
            if (!CryptographicOperations.FixedTimeEquals(
                    HMACSHA256.HashData(signingKey, payload), signature))
                return false;

            var ticket = JsonSerializer.Deserialize<UploadTicket>(payload);
            if (ticket is null || ticket.JobId != jobId ||
                ticket.TechnicianId != technicianId || ticket.Kind != kind ||
                ticket.ExpiresAtUtc <= DateTimeOffset.UtcNow ||
                string.IsNullOrWhiteSpace(ticket.BlobName))
                return false;

            blobName = ticket.BlobName;
            return true;
        }
        catch (Exception ex) when (ex is FormatException or JsonException or ArgumentException)
        {
            return false;
        }
    }

    private sealed record UploadTicket(string BlobName, Guid JobId,
        Guid TechnicianId, MobileUploadKind Kind, DateTimeOffset ExpiresAtUtc);
}
