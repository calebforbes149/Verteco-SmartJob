namespace VertecoSmartJob.Contracts.Jobs;

// Sent by the mobile app when it needs an upload link
public sealed class MobileEvidenceUploadRequest
{
    public string Extension { get; init; } = string.Empty;
}

// Temporary upload details returned to the mobile app
public sealed class MobileEvidenceUploadResponse
{
    public string UploadToken { get; init; } = string.Empty;

    public string UploadUrl { get; init; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; init; }
}

// Sent after the mobile app has uploaded the evidence file
public sealed class MobileEvidenceConfirmRequest
{
    public string UploadToken { get; init; } = string.Empty;

    public string Sha256 { get; init; } = string.Empty;

    public string Caption { get; init; } = string.Empty;
}