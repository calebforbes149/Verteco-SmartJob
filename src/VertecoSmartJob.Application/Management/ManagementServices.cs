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

public sealed class UserManagementService(IUserRepository users, IAuditLogRepository audit, IUnitOfWork work, ICurrentUser? currentUser = null)
{
    public async Task<User> CreateAsync(User user, CancellationToken ct = default)
    {
        await users.AddAsync(user, ct);
        await audit.AddAsync(new AuditLog { ActorUserId = currentUser?.UserId, Action = "UserCreated", Details = user.Id.ToString() }, ct);
        await work.SaveChangesAsync(ct);
        return user;
    }

    public async Task<User> UpdateAsync(Guid id, Action<User> update, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("User not found.");
        update(user);
        await audit.AddAsync(new AuditLog { ActorUserId = currentUser?.UserId, Action = user.IsDeleted ? "UserDeactivated" : user.IsSuspended ? "UserSuspended" : "UserUpdated", Details = user.Id.ToString() }, ct);
        await work.SaveChangesAsync(ct);
        return user;
    }

    public Task<User> SuspendAsync(Guid id, CancellationToken ct = default) => UpdateAsync(id, u => u.Suspend(), ct);
    public Task<User> ReactivateAsync(Guid id, CancellationToken ct = default) => UpdateAsync(id, u => u.Reactivate(), ct);
    public Task<User> DeactivateAsync(Guid id, CancellationToken ct = default) => UpdateAsync(id, u => u.SoftDelete(), ct);
}
