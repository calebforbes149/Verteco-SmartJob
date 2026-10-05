using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VertecoSmartJob.Infrastructure.Persistence;

namespace VertecoSmartJob.Tests.Integration;

public sealed class StandardTemplateMigrationTests
{
    [Fact]
    public void SqlServerMigration_IsDiscoverableAndGeneratesOnlyBaselineTemplateData()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=MigrationScriptOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new AppDbContext(options);

        const string migrationId = "20261003120000_AddStandardJobCardTemplate";
        Assert.Contains(migrationId, db.Database.GetMigrations());

        var sql = db.GetService<IMigrator>().GenerateScript(
            "20261001225759_AddPasswordResetTokens", migrationId,
            MigrationsSqlGenerationOptions.Idempotent);

        Assert.Contains("Standard Job Card", sql);
        Assert.Contains("TemplateChecklistItems", sql);
        Assert.DoesNotContain("INSERT INTO [dbo].[Users]", sql);
        Assert.DoesNotContain("INSERT INTO [dbo].[JobCards]", sql);
    }
}
