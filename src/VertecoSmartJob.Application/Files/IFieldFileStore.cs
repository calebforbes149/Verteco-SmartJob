namespace VertecoSmartJob.Application.Files;

// Handles storing and checking evidence files
public interface IFieldFileStore
{
    Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken ct = default);

    Task DeleteAsync(
        string reference,
        CancellationToken ct = default);

    Task<FieldStoredFile?> ReadAsync(
        string reference,
        CancellationToken ct = default);

    // Creates a temporary upload link for the mobile app
    Task<FieldUploadInstruction> CreateUploadInstructionAsync(
        string extension,
        CancellationToken ct = default);

    // Checks the uploaded file before confirming the evidence
    Task<string> ConfirmUploadAsync(
        string uploadToken,
        string expectedSha256,
        CancellationToken ct = default);
}

public sealed record FieldStoredFile(Stream Content, string ContentType);

// Holds the temporary upload details sent to the mobile app
public sealed class FieldUploadInstruction
{
    public string UploadToken { get; init; } = string.Empty;

    public string UploadUrl { get; init; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; init; }
}