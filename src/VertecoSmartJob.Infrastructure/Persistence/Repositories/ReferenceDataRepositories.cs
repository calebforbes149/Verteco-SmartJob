using Microsoft.EntityFrameworkCore;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Infrastructure.Persistence.Repositories;

public class CustomerRepository(AppDbContext db) : ICustomerRepository
{
    public async Task AddAsync(Customer customer, CancellationToken ct = default) => await db.Customers.AddAsync(customer, ct);
    public async Task<List<Customer>> ListAsync(string? q = null, CancellationToken ct = default)
    {
        var query = db.Customers.Include(c => c.Sites).ThenInclude(s => s.Equipment).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(needle));
        }
        return await query.OrderBy(c => c.Name).ToListAsync(ct);
    }

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Customers.Include(c => c.Sites).ThenInclude(s => s.Equipment).FirstOrDefaultAsync(c => c.Id == id, ct);
}

public class SiteRepository(AppDbContext db) : ISiteRepository
{
    public async Task AddAsync(Site site, CancellationToken ct = default) => await db.Sites.AddAsync(site, ct);

    public Task<List<Site>> ListAsync(string? q = null, CancellationToken ct = default)
    {
        var query = db.Sites.Include(s => s.Customer).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(s => s.Name.ToLower().Contains(q.Trim().ToLower()));
        return query.OrderBy(s => s.Name).ToListAsync(ct);
    }
    public Task<List<Site>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        db.Sites.Where(s => s.CustomerId == customerId).Include(s => s.Equipment).OrderBy(s => s.Name).ToListAsync(ct);

    public Task<Site?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Sites.Include(s => s.Customer).Include(s => s.Equipment).Include(s => s.JobCards)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Equipment?> FindBySerialNumberAsync(string serialNumber, CancellationToken ct = default) =>
        db.Equipment.Include(e => e.Site).ThenInclude(s => s!.Customer)
            .FirstOrDefaultAsync(e => e.SerialNumber == serialNumber, ct);
}

public class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task AddAsync(User user, CancellationToken ct = default) => await db.Users.AddAsync(user, ct);
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) => db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) => db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
    public Task<User?> GetByTechnicianIdAsync(Guid technicianId, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.TechnicianId == technicianId, ct);
    public Task<List<User>> ListAsync(string? q = null, CancellationToken ct = default)
    {
        var query = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(u => u.FullName.ToLower().Contains(q.Trim().ToLower()));
        return query.OrderBy(u => u.FullName).ToListAsync(ct);
    }
}

public class AuditLogRepository(AppDbContext db) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog entry, CancellationToken ct = default) => await db.AuditLogs.AddAsync(entry, ct);
    public Task<List<AuditLog>> ListAsync(CancellationToken ct = default) => db.AuditLogs.AsNoTracking().OrderByDescending(a => a.CreatedAtUtc).Take(1000).ToListAsync(ct);
}

public class TechnicianRepository(AppDbContext db) : ITechnicianRepository
{
    public async Task AddAsync(Technician technician, CancellationToken ct = default) => await db.Technicians.AddAsync(technician, ct);

    public Task<List<Technician>> ListAsync(CancellationToken ct = default) =>
        db.Technicians.OrderBy(t => t.FullName).ToListAsync(ct);

    public Task<Technician?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Technicians.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Technician?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.Technicians.FirstOrDefaultAsync(t => t.Email == email, ct);
}

public class JobTemplateRepository(AppDbContext db) : IJobTemplateRepository
{
    public Task<List<JobTemplate>> ListAsync(CancellationToken ct = default) =>
        db.JobTemplates.Where(t => t.Name == "Standard Job Card").Include(t => t.ChecklistItems).OrderBy(t => t.Name).ToListAsync(ct);

    public Task<JobTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.JobTemplates.Where(t => t.Name == "Standard Job Card").Include(t => t.ChecklistItems).FirstOrDefaultAsync(t => t.Id == id, ct);
}

public class PasswordResetTokenRepository(AppDbContext db) : IPasswordResetTokenRepository
{
    public async Task AddAsync(PasswordResetToken token, CancellationToken ct = default) =>
        await db.PasswordResetTokens.AddAsync(token, ct);

    public Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        db.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
}