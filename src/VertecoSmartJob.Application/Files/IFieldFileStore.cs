namespace VertecoSmartJob.Application.Files;

public interface IFieldFileStore
{
    Task<string> SaveAsync(Stream content, string extension, CancellationToken ct = default);
    Task DeleteAsync(string reference, CancellationToken ct = default);
}
