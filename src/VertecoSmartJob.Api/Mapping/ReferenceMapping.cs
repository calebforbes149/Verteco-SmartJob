using VertecoSmartJob.Contracts.Reference;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Api.Mapping;

public static class ReferenceMapping
{
    public static UserDto ToDto(this User u) => new()
    {
        Id = u.Id, FullName = u.FullName, Email = u.Email, Phone = u.Phone,
        Role = u.Role, TechnicianId = u.TechnicianId, IsSuspended = u.IsSuspended
    };
    public static CustomerDto ToDto(this Customer c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Industry = c.Industry,
        ContactName = c.ContactName,
        ContactEmail = c.ContactEmail,
        ContactPhone = c.ContactPhone,
        Sites = c.Sites.Select(s => s.ToDto()).ToList()
    };

    public static SiteDto ToDto(this Site s) => new()
    {
        Id = s.Id,
        CustomerId = s.CustomerId,
        CustomerName = s.Customer?.Name ?? "",
        Name = s.Name,
        Address = s.Address,
        SiteType = s.SiteType,
        Equipment = s.Equipment.Select(e => new EquipmentDto { Id = e.Id, Name = e.Name, SerialNumber = e.SerialNumber, Description = e.Description }).ToList()
    };

    public static TechnicianDto ToDto(this Technician t, Guid? userId = null) => new()
    {
        Id = t.Id,
        UserId = userId,
        FullName = t.FullName,
        Email = t.Email,
        Phone = t.Phone,
        Skills = t.Skills,
        Availability = t.Availability.ToString(),
        JobsCompleted = t.JobsCompleted
    };

    public static JobTemplateDto ToDto(this JobTemplate t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Category = t.Category,
        Description = t.Description,
        TimesUsed = t.TimesUsed,
        ChecklistItems = t.ChecklistItems.OrderBy(i => i.Order).Select(i => i.Text).ToList()
    };
}
