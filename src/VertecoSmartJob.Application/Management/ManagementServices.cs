using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Application.Management;

public sealed class CustomerService(ICustomerRepository customers, IAuditLogRepository audit, IUnitOfWork work, ICurrentUser? currentUser = null)
{
    public async Task<Customer> CreateAsync(Customer customer, CancellationToken ct = default)
    {
        await customers.AddAsync(customer, ct);
        await audit.AddAsync(new AuditLog { ActorUserId = currentUser?.UserId, Action = "CustomerCreated", Details = customer.Id.ToString() }, ct);
        await work.SaveChangesAsync(ct);
        return customer;
    }

    public async Task<Customer> UpdateAsync(Guid id, Action<Customer> update, CancellationToken ct = default)
    {
        var customer = await customers.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Customer not found.");
        update(customer);
        await audit.AddAsync(new AuditLog { ActorUserId = currentUser?.UserId, Action = "CustomerUpdated", Details = customer.Id.ToString() }, ct);
        await work.SaveChangesAsync(ct);
        return customer;
    }
}

public sealed class SiteService(ISiteRepository sites, ICustomerRepository customers, IAuditLogRepository audit, IUnitOfWork work, ICurrentUser? currentUser = null)
{
    public async Task<Site> CreateAsync(Site site, CancellationToken ct = default)
    {
        if (await customers.GetByIdAsync(site.CustomerId, ct) is null)
            throw new KeyNotFoundException("Customer not found.");
        await sites.AddAsync(site, ct);
        await audit.AddAsync(new AuditLog { ActorUserId = currentUser?.UserId, Action = "SiteCreated", Details = site.Id.ToString() }, ct);
        await work.SaveChangesAsync(ct);
        return site;
    }

    public async Task<Site> UpdateAsync(Guid id, Action<Site> update, CancellationToken ct = default)
    {
        var site = await sites.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Site not found.");
        update(site);
        await audit.AddAsync(new AuditLog { ActorUserId = currentUser?.UserId, Action = "SiteUpdated", Details = site.Id.ToString() }, ct);
        await work.SaveChangesAsync(ct);
        return site;
    }
}

public sealed class TechnicianManagementService(
    ITechnicianRepository technicians, IUserRepository users, IAuditLogRepository audit,
    IUnitOfWork work, ICurrentUser? currentUser = null)
{
    public async Task<(Technician Technician, User Account)> CreateAsync(Technician technician, CancellationToken ct = default)
    {
        if (await technicians.GetByEmailAsync(technician.Email, ct) is not null ||
            await users.GetByEmailAsync(technician.Email, ct) is not null)
            throw new ArgumentException("That email is already in use.");

        var account = new User
        {
            FullName = technician.FullName, Email = technician.Email, Phone = technician.Phone,
            Role = "Technician", TechnicianId = technician.Id, Technician = technician
        };
        await technicians.AddAsync(technician, ct);
        await users.AddAsync(account, ct);
        await audit.AddAsync(new AuditLog
        {
            ActorUserId = currentUser?.UserId, Action = "TechnicianCreated", Details = technician.Id.ToString()
        }, ct);
        await work.SaveChangesAsync(ct);
        return (technician, account);
    }

    public async Task<(Technician Technician, User? Account)> UpdateAsync(
        Guid id, Action<Technician> update, CancellationToken ct = default)
    {
        var technician = await technicians.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Technician not found.");
        update(technician);
        var account = await users.GetByTechnicianIdAsync(id, ct);
        if (account is not null)
        {
            account.FullName = technician.FullName;
            account.Email = technician.Email;
            account.Phone = technician.Phone;
        }
        await audit.AddAsync(new AuditLog
        {
            ActorUserId = currentUser?.UserId, Action = "TechnicianUpdated", Details = id.ToString()
        }, ct);
        await work.SaveChangesAsync(ct);
        return (technician, account);
    }
}

public sealed class UserManagementService(
    IUserRepository users, ITechnicianRepository technicians, IAuditLogRepository audit,
    IUnitOfWork work, ICurrentUser? currentUser = null)
{
    public async Task<User> CreateAsync(User user, CancellationToken ct = default)
    {
        if (user.Role == "Technician")
        {
            if (user.TechnicianId is not { } technicianId || await technicians.GetByIdAsync(technicianId, ct) is null)
                throw new KeyNotFoundException("Technician not found.");
            if (await users.GetByTechnicianIdAsync(technicianId, ct) is not null)
                throw new ArgumentException("This technician already has a user account. Use POST /api/v1/technicians for a new technician.");
        }
        await users.AddAsync(user, ct);
        await audit.AddAsync(new AuditLog { ActorUserId = currentUser?.UserId, Action = "UserCreated", Details = user.Id.ToString() }, ct);
        await work.SaveChangesAsync(ct);
        return user;
    }

    public async Task<User> UpdateAsync(Guid id, Action<User> update, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("User not found.");
        update(user);
        if (user.Role == "Technician" && user.TechnicianId is { } technicianId)
        {
            var technician = await technicians.GetByIdAsync(technicianId, ct);
            if (technician is not null)
            {
                technician.FullName = user.FullName;
                technician.Email = user.Email;
                technician.Phone = user.Phone;
            }
        }
        await audit.AddAsync(new AuditLog { ActorUserId = currentUser?.UserId, Action = user.IsDeleted ? "UserDeactivated" : user.IsSuspended ? "UserSuspended" : "UserUpdated", Details = user.Id.ToString() }, ct);
        await work.SaveChangesAsync(ct);
        return user;
    }

    public Task<User> SuspendAsync(Guid id, CancellationToken ct = default) => UpdateAsync(id, u => u.Suspend(), ct);
    public Task<User> ReactivateAsync(Guid id, CancellationToken ct = default) => UpdateAsync(id, u => u.Reactivate(), ct);
    public Task<User> DeactivateAsync(Guid id, CancellationToken ct = default) => UpdateAsync(id, u => u.SoftDelete(), ct);
}
