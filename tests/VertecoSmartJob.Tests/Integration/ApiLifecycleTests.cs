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
using VertecoSmartJob.Application.Jobs;
using VertecoSmartJob.Domain.Entities;
using VertecoSmartJob.Infrastructure.Persistence;
using VertecoSmartJob.Infrastructure.Persistence.Seed;

namespace VertecoSmartJob.Tests.Integration;

public sealed class ApiLifecycleTests
{
    [Fact]
    public async Task ManagerApi_CreateAndApprove_UsesLifecycleServiceAndPersistsRevision()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
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

        using var anonymous = factory.CreateClient();
        using (var seedScope = factory.Services.CreateScope())
            await DbSeeder.SeedAsync(seedScope.ServiceProvider.GetRequiredService<AppDbContext>());
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/jobs")).StatusCode);

        using var manager = factory.CreateClient();
        manager.DefaultRequestHeaders.Add("X-SmartJob-Key", "integration-manager-key");
        using var technician = factory.CreateClient();
        technician.DefaultRequestHeaders.Add("X-SmartJob-Key", "integration-technician-key");

        var customers = await manager.GetFromJsonAsync<PagedResponse<CustomerDto>>("/api/v1/customers?pageSize=1");
        var customer = Assert.Single(customers!.Items);
        var techs = await manager.GetFromJsonAsync<PagedResponse<TechnicianDto>>("/api/v1/technicians");
        var tech = Assert.Single(techs!.Items, t => t.Email == "thabo.nkosi@verteco.co.za");
        var create = new CreateJobCardRequest
        {
            CustomerId = customer.Id, SiteId = customer.Sites[0].Id, TechnicianId = tech.Id,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow), Notes = "Integration test"
        };
        var forbidden = await technician.PostAsJsonAsync("/api/v1/jobs", create);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var created = await manager.PostAsJsonAsync("/api/v1/jobs", create);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var job = await created.Content.ReadFromJsonAsync<JobCardDetailDto>();
        Assert.NotNull(job);
        Assert.Equal("Assigned", job.Status);
        Assert.Equal("Standard Job Card", job.TemplateName);

        // Person 4 owns technician HTTP actions and upload/confirmation. Person 1 provides
        // the guarded lifecycle service that those future endpoints will call.
        using (var scope = factory.Services.CreateScope())
        {
            var jobs = scope.ServiceProvider.GetRequiredService<JobCardService>();
            await jobs.AcceptAsync(job.Id, tech.Id);
            await jobs.StartAsync(job.Id, tech.Id);
            foreach (var item in job.ChecklistItems)
                await jobs.ToggleChecklistItemAsync(job.Id, item.Id, tech.Id);
            await jobs.AddEvidenceAsync(job.Id, tech.Id, "blob:fixture.jpg:sha256:TEST", "Site photo");
            await jobs.AddSignatureAsync(job.Id, tech.Id, tech.FullName, "Technician", "blob:tech.png:sha256:TEST");
            await jobs.AddSignatureAsync(job.Id, tech.Id, "Site representative", "Client", "blob:client.png:sha256:TEST");
            await jobs.SubmitForReviewAsync(job.Id, tech.Id);
        }
        var submittedJob = await manager.GetFromJsonAsync<JobCardDetailDto>($"/api/v1/jobs/{job.Id}");
        Assert.True(Assert.Single(submittedJob!.Revisions).IntegrityValid);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await technician.PostAsJsonAsync($"/api/v1/jobs/{job.Id}/approve", new { reviewNotes = "Approved" })).StatusCode);

        var approved = await manager.PostAsJsonAsync($"/api/v1/jobs/{job.Id}/approve", new { reviewNotes = "Approved" });
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        Assert.Equal("Completed", (await approved.Content.ReadFromJsonAsync<JobCardDetailDto>())!.Status);

        using var pdf = await manager.GetAsync($"/api/v1/reports/{DateTime.UtcNow:yyyy-MM}/export?format=pdf");
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", pdf.Content.Headers.ContentDisposition?.DispositionType);
        var document = System.Text.Encoding.ASCII.GetString(await pdf.Content.ReadAsByteArrayAsync());
        Assert.StartsWith("%PDF-1.4", document);
        Assert.Contains("/Count 2", document);
        Assert.Contains("/Subtype /Image", document);
        Assert.Contains("/Logo Do", document);
        Assert.Contains("AI commentary", document);
        Assert.Contains("Unavailable for this report", document);
    }
}
