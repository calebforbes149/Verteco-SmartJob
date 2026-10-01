using System.Security.Cryptography;
using System.Text;
using VertecoSmartJob.Domain.Common;

namespace VertecoSmartJob.Domain.Entities;

public class JobCardRevision : Entity
{
    public Guid JobCardId { get; private set; }
    public JobCard? JobCard { get; private set; }
    public int Number { get; private set; }
    public Guid? PreviousRevisionId { get; private set; }
    public string ContentJson { get; private set; } = string.Empty;
    public string ContentHash { get; private set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; private set; }

    private JobCardRevision() { }

    public static JobCardRevision Create(Guid jobCardId, int number, string content, Guid? previousRevisionId = null)
    {
        return new JobCardRevision
        {
            JobCardId = jobCardId,
            Number = number,
            PreviousRevisionId = previousRevisionId,
            ContentJson = content,
            ContentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))),
            SubmittedAtUtc = DateTime.UtcNow
        };
    }

    public bool VerifyIntegrity() =>
        ContentHash == Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ContentJson)));
}

public class JobEvidence : Entity
{
    public Guid JobCardId { get; set; }
    public JobCard? JobCard { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
}

public class JobSignature : Entity
{
    public Guid JobCardId { get; set; }
    public JobCard? JobCard { get; set; }
    public string Type { get; set; } = string.Empty;
    public string SignerName { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
}
