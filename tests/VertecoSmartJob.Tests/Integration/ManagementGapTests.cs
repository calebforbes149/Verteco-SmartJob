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
using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Jobs;
using VertecoSmartJob.Contracts.Reference;
using VertecoSmartJob.Infrastructure.Persistence;

namespace VertecoSmartJob.Tests.Integration;

public sealed class ManagementGapTests
{
    [Fact]
    public async Task CreateTechnician_CreatesAccountAndKeepsEditsInSync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var factory = CreateFactory(connection);
        using var manager = Client(factory, "integration-manager-key");
        using var technician = Client(factory, "integration-thabo-key");

        var request = new CreateTechnicianRequest
        {
            FullName = "Test Technician", Email = "new.technician@example.test", Phone = "011 123 4567",
            Skills = ["Power Systems"], Availability = "Available"
        };
        Assert.Equal(HttpStatusCode.Forbidden,
            (await technician.PostAsJsonAsync("/api/v1/technicians", request)).StatusCode);

        var created = await manager.PostAsJsonAsync("/api/v1/technicians", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var tech = (await created.Content.ReadFromJsonAsync<TechnicianDto>())!;
        Assert.NotNull(tech.UserId);
        var account = await manager.GetFromJsonAsync<UserDto>($"/api/v1/users/{tech.UserId}");
        Assert.Equal(tech.Id, account!.TechnicianId);
        Assert.Equal("new.technician@example.test", account.Email);

        var patched = await manager.PatchAsJsonAsync($"/api/v1/technicians/{tech.Id}",
            new { fullName = "Updated Technician", skills = new[] { "Networking" } });
        Assert.Equal(HttpStatusCode.OK, patched.StatusCode);
        var updated = (await patched.Content.ReadFromJsonAsync<TechnicianDto>())!;
        Assert.Equal("Updated Technician", updated.FullName);
        Assert.Contains("Networking", updated.Skills);
        account = await manager.GetFromJsonAsync<UserDto>($"/api/v1/users/{tech.UserId}");
        Assert.Equal("Updated Technician", account!.FullName);

        var listed = await manager.GetFromJsonAsync<PagedResponse<TechnicianDto>>("/api/v1/technicians?q=Updated%20Technician");
        Assert.Equal(tech.UserId, Assert.Single(listed!.Items).UserId);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await manager.PostAsJsonAsync("/api/v1/technicians", request)).StatusCode);
    }

    [Fact]
    public async Task FlaggedJobs_AppearInCorrectiveActionsAndCanBeResolved()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var factory = CreateFactory(connection);
        using var manager = Client(factory, "integration-manager-key");
        using var assigned = Client(factory, "integration-thabo-key");
        using var otherTechnician = Client(factory, "integration-sipho-key");

        var customers = await manager.GetFromJsonAsync<PagedResponse<CustomerDto>>("/api/v1/customers?pageSize=1");
        var customer = Assert.Single(customers!.Items);
        var techs = await manager.GetFromJsonAsync<PagedResponse<TechnicianDto>>("/api/v1/technicians");
        var thabo = Assert.Single(techs!.Items, t => t.Email == "thabo.nkosi@verteco.co.za");
        var created = await manager.PostAsJsonAsync("/api/v1/jobs", new CreateJobCardRequest
        {
            CustomerId = customer.Id, SiteId = customer.Sites[0].Id, TechnicianId = thabo.Id,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow), Notes = "Flag test"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var job = (await created.Content.ReadFromJsonAsync<JobCardDetailDto>())!;

        Assert.Equal(HttpStatusCode.Forbidden,
            (await otherTechnician.PostAsJsonAsync($"/api/v1/jobs/{job.Id}/flag", new { reason = "Damage" })).StatusCode);
        var flagged = await assigned.PostAsJsonAsync($"/api/v1/jobs/{job.Id}/flag", new { reason = "Damage" });
        Assert.True(flagged.StatusCode == HttpStatusCode.OK, await flagged.Content.ReadAsStringAsync());
        job = (await flagged.Content.ReadFromJsonAsync<JobCardDetailDto>())!;
        Assert.True(job.IsFlagged);
        var action = Assert.Single(job.CorrectiveActions);
        Assert.True(action.IsOpen);

        var flaggedJobs = await manager.GetFromJsonAsync<PagedResponse<JobCardSummaryDto>>("/api/v1/jobs?flagged=true");
        Assert.Contains(flaggedJobs!.Items, j => j.Id == job.Id && j.IsFlagged);
        var openActions = await manager.GetFromJsonAsync<PagedResponse<CorrectiveActionDto>>("/api/v1/corrective-actions?status=open");
        Assert.Contains(openActions!.Items, a => a.Id == action.Id);
        Assert.Equal(HttpStatusCode.Conflict,
            (await manager.PostAsJsonAsync($"/api/v1/jobs/{job.Id}/flag", new { reason = "Again" })).StatusCode);

        var resolved = await manager.PostAsJsonAsync($"/api/v1/corrective-actions/{action.Id}/resolve",
            new { resolutionNotes = "Repaired and retested" });
        Assert.Equal(HttpStatusCode.OK, resolved.StatusCode);
        Assert.False((await resolved.Content.ReadFromJsonAsync<CorrectiveActionDto>())!.IsOpen);
        var details = await manager.GetFromJsonAsync<JobCardDetailDto>($"/api/v1/jobs/{job.Id}");
        Assert.False(details!.IsFlagged);
        Assert.Equal("Repaired and retested", Assert.Single(details.CorrectiveActions).ResolutionNotes);
        var closedActions = await manager.GetFromJsonAsync<PagedResponse<CorrectiveActionDto>>(
            $"/api/v1/corrective-actions?status=resolved&jobId={job.Id}");
        Assert.Equal(action.Id, Assert.Single(closedActions!.Items).Id);
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
                    ["ApiKeys:Accounts:1:Key"] = "integration-thabo-key",
                    ["ApiKeys:Accounts:2:Email"] = "sipho.dlamini@verteco.co.za",
                    ["ApiKeys:Accounts:2:Key"] = "integration-sipho-key"
                }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
            });
        });

    private static HttpClient Client(WebApplicationFactory<Program> factory, string key)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-SmartJob-Key", key);
        return client;
    }
}
