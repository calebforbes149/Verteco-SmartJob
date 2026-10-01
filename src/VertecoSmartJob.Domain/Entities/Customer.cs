using VertecoSmartJob.Domain.Common;

namespace VertecoSmartJob.Domain.Entities;

public class Customer : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;

    public List<Site> Sites { get; set; } = [];
}
