namespace VertecoSmartJob.Contracts.Reference;

public class CustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public List<SiteDto> Sites { get; set; } = [];
}

public class CreateCustomerRequest
{
    public string Name { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
}

public class PatchCustomerRequest
{
    public string? Name { get; set; }
    public string? Industry { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

public class SiteDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string SiteType { get; set; } = string.Empty;
    public List<EquipmentDto> Equipment { get; set; } = [];
}

public class CreateSiteRequest
{
    public Guid CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string SiteType { get; set; } = string.Empty;
}

public class PatchSiteRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? SiteType { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? TechnicianId { get; set; }
    public bool IsSuspended { get; set; }
}

public class CreateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = "Technician";
    public Guid? TechnicianId { get; set; }
}

public class PatchUserRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class EquipmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class TechnicianDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = [];
    public string Availability { get; set; } = string.Empty;
    public int JobsCompleted { get; set; }
}

public class CreateTechnicianRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = [];
    public string Availability { get; set; } = "Available";
}

public class PatchTechnicianRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public List<string>? Skills { get; set; }
    public string? Availability { get; set; }
}

public class JobTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TimesUsed { get; set; }
    public List<string> ChecklistItems { get; set; } = [];
}

public class DashboardStatsDto
{
    public int ActiveJobCards { get; set; }
    public int DueTodayCount { get; set; }
    public int FlaggedJobCards { get; set; }
    public int TechniciansAvailable { get; set; }
}
