using VertecoSmartJob.Domain.Common;

namespace VertecoSmartJob.Domain.Entities;

/// <summary>
/// Equipment.SerialNumber is indexed and searchable per the Part 2 requirement that Verteco
/// can look up warranty history by serial number, since job card data is retained indefinitely.
/// </summary>
public class Equipment : Entity
{
    public Guid SiteId { get; set; }
    public Site? Site { get; set; }

    public string Name { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string? Description { get; set; }
}
