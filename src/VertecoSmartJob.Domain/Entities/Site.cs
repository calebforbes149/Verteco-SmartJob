using VertecoSmartJob.Domain.Common;

namespace VertecoSmartJob.Domain.Entities;

public class Site : SoftDeletableEntity
{
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string SiteType { get; set; } = string.Empty;

    public List<Equipment> Equipment { get; set; } = [];
    public List<JobCard> JobCards { get; set; } = [];
}
