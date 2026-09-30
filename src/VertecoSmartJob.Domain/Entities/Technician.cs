using VertecoSmartJob.Domain.Common;
using VertecoSmartJob.Domain.Enums;

namespace VertecoSmartJob.Domain.Entities;

public class Technician : SoftDeletableEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = [];
    public TechnicianAvailability Availability { get; set; } = TechnicianAvailability.Available;
    public int JobsCompleted { get; set; }

    public List<JobCard> JobCards { get; set; } = [];
}
