using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VertecoSmartJob.Contracts.Auth;
using VertecoSmartJob.Contracts.Reference;
using VertecoSmartJob.Infrastructure.Identity;
using VertecoSmartJob.Infrastructure.Persistence;
using VertecoSmartJob.Infrastructure.Persistence.Seed;

namespace VertecoSmartJob.Tests.Integration;

public sealed class AccountProfileTests
{
    [Fact]
    public async Task ForgotPasswordWithoutMailConfiguration_DoesNotClaimAResetLinkWasSent()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var factory = CreateFactory(connection);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new ForgotPasswordRequest { Email = "someone@example.test" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task ManagerCanEditOwnProfile_ButEmailChangeRequiresPasswordAndUniqueAddress()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var factory = CreateFactory(connection);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await DbSeeder.SeedAsync(db);
            var user = await db.Users.SingleAsync(u => u.Email == "manager@verteco.example");
            var hasher = scope.ServiceProvider.GetRequiredService<PasswordHashingService>();
            user.SetPasswordHash(hasher.Hash(user, "original-password"));
            await db.SaveChangesAsync();
        }

        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.GetAsync("/api/v1/account/profile")).StatusCode);

        using var manager = factory.CreateClient();
        manager.DefaultRequestHeaders.Add("X-SmartJob-Key", "integration-manager-key");
        var current = await manager.GetFromJsonAsync<UserDto>("/api/v1/account/profile");
        Assert.Equal("manager@verteco.example", current!.Email);

        var request = new UpdateOwnProfileRequest
        {
            FullName = "Updated Manager",
            Email = "new-manager@example.test",
            Phone = "012 345 6789"
        };
        Assert.Equal(HttpStatusCode.BadRequest,
            (await manager.PatchAsJsonAsync("/api/v1/account/profile", request)).StatusCode);

        request.CurrentPassword = "original-password";
        request.Email = "thabo.nkosi@verteco.co.za";
        Assert.Equal(HttpStatusCode.Conflict,
            (await manager.PatchAsJsonAsync("/api/v1/account/profile", request)).StatusCode);

        request.Email = "new-manager@example.test";
        var saved = await manager.PatchAsJsonAsync("/api/v1/account/profile", request);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var updated = await saved.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal(current.Id, updated!.Id);
        Assert.Equal("Updated Manager", updated.FullName);
        Assert.Equal("new-manager@example.test", updated.Email);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("012 345 6789",
            (await verifyDb.Users.SingleAsync(u => u.Id == current.Id)).Phone);
        Assert.Contains(await verifyDb.AuditLogs.ToListAsync(),
            a => a.Action == "OwnProfileUpdated" && a.ActorUserId == current.Id);
    }

    [Fact]
    public async Task TechnicianProfileEdit_UpdatesLinkedTechnician()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var factory = CreateFactory(connection);
        using (var seedScope = factory.Services.CreateScope())
            await DbSeeder.SeedAsync(seedScope.ServiceProvider.GetRequiredService<AppDbContext>());

        using var technician = factory.CreateClient();
        technician.DefaultRequestHeaders.Add("X-SmartJob-Key", "integration-technician-key");
        var saved = await technician.PatchAsJsonAsync("/api/v1/account/profile",
            new UpdateOwnProfileRequest
            {
                FullName = "Thabo Updated",
                Email = "thabo.nkosi@verteco.co.za",
                Phone = "071 000 0000"
            });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == "thabo.nkosi@verteco.co.za");
        var linked = await db.Technicians.SingleAsync(t => t.Id == user.TechnicianId);
        Assert.Equal("Thabo Updated", linked.FullName);
        Assert.Equal("071 000 0000", linked.Phone);
    }

    private static WebApplicationFactory<Program> CreateFactory(SqliteConnection connection) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:AzureSql"] = "Server=localhost;Database=SmartJobTest;User Id=x;Password=y;TrustServerCertificate=true",
                    ["ApiKeys:Accounts:0:Email"] = "manager@verteco.example",
                    ["ApiKeys:Accounts:0:Key"] = "integration-manager-key",
                    ["ApiKeys:Accounts:1:Email"] = "thabo.nkosi@verteco.co.za",
                    ["ApiKeys:Accounts:1:Key"] = "integration-technician-key"
                }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
            });
        });
}
