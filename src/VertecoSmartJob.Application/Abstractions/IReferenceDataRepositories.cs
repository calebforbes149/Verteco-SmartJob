using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Application.Abstractions;

public interface ICustomerRepository
{
    Task<List<Customer>> ListAsync(
        string? q = null,
        CancellationToken ct = default);

    Task<Customer?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task AddAsync(
        Customer customer,
        CancellationToken ct = default);
}

public interface ISiteRepository
{
    Task<List<Site>> ListByCustomerAsync(
        Guid customerId,
        CancellationToken ct = default);

    Task<List<Site>> ListAsync(
        string? q = null,
        CancellationToken ct = default);

    Task<Site?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<Equipment?> FindBySerialNumberAsync(
        string serialNumber,
        CancellationToken ct = default);

    // Adds equipment captured by a technician
    Task AddEquipmentAsync(
        Equipment equipment,
        CancellationToken ct = default);

    Task AddAsync(
        Site site,
        CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<List<User>> ListAsync(
        string? q = null,
        CancellationToken ct = default);

    Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<User?> GetByEmailAsync(
        string email,
        CancellationToken ct = default);

    Task<User?> GetByTechnicianIdAsync(
        Guid technicianId,
        CancellationToken ct = default);

    Task AddAsync(
        User user,
        CancellationToken ct = default);
}

public interface IAuditLogRepository
{
    Task AddAsync(
        AuditLog entry,
        CancellationToken ct = default);

    Task<List<AuditLog>> ListAsync(
        CancellationToken ct = default);
}

public interface ITechnicianRepository
{
    Task<List<Technician>> ListAsync(
        CancellationToken ct = default);

    Task<Technician?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<Technician?> GetByEmailAsync(
        string email,
        CancellationToken ct = default);

    Task AddAsync(
        Technician technician,
        CancellationToken ct = default);
}

public interface IJobTemplateRepository
{
    Task<List<JobTemplate>> ListAsync(
        CancellationToken ct = default);

    Task<JobTemplate?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);
}

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?>
        GetByTokenHashAsync(
            string tokenHash,
            CancellationToken ct = default);

    Task AddAsync(
        PasswordResetToken token,
        CancellationToken ct = default);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?>
        GetByTokenHashAsync(
            string tokenHash,
            CancellationToken ct = default);

    Task AddAsync(
        RefreshToken token,
        CancellationToken ct = default);
}