using VertecoSmartJob.Contracts.Reference;

namespace VertecoSmartJob.Web.Models;

public class TechnicianDetailsViewModel
{
    public TechnicianDto Technician { get; set; } = new();

    public UserDto? Account { get; set; }
}