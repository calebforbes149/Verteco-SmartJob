using Microsoft.EntityFrameworkCore;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository(
    AppDbContext db)
    : IRefreshTokenRepository
{
    public async Task<RefreshToken?>
        GetByTokenHashAsync(
            string tokenHash,
            CancellationToken ct = default)
    {
        return await db.RefreshTokens
            .SingleOrDefaultAsync(
                x => x.TokenHash == tokenHash,
                ct);
    }

    public async Task AddAsync(
        RefreshToken token,
        CancellationToken ct = default)
    {
        await db.RefreshTokens.AddAsync(
            token,
            ct);
    }
}