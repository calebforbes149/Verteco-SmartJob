using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace VertecoSmartJob.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261003120000_AddStandardJobCardTemplate")]
public sealed class AddStandardJobCardTemplate : Migration
{
    private const string TemplateId = "7c59055c-7d5a-4a02-9191-e95729b23bab";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // This is the one required job definition, separate from the demo fixtures.
        // Existing databases that already have an active standard template keep it.
        migrationBuilder.Sql($$"""
            IF NOT EXISTS (
                SELECT 1 FROM [dbo].[JobTemplates] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Name] = N'Standard Job Card' AND [IsDeleted] = 0
            )
            BEGIN
                IF EXISTS (SELECT 1 FROM [dbo].[JobTemplates] WHERE [Id] = '{{TemplateId}}')
                    THROW 51210, 'Reserved Standard Job Card template ID is already in use.', 1;

                INSERT INTO [dbo].[JobTemplates]
                    ([Id], [Name], [Category], [Description], [TimesUsed], [RowVersion], [IsDeleted], [DeletedAtUtc])
                VALUES
                    ('{{TemplateId}}', N'Standard Job Card', N'All field work',
                     N'The single standard checklist used for every Verteco job card.',
                     0, CONVERT(varbinary(16), NEWID()), 0, NULL);

                INSERT INTO [dbo].[TemplateChecklistItems]
                    ([Id], [JobTemplateId], [Order], [Text], [RowVersion])
                VALUES
                    (NEWID(), '{{TemplateId}}', 0, N'Confirm site access and safety controls', CONVERT(varbinary(16), NEWID())),
                    (NEWID(), '{{TemplateId}}', 1, N'Record equipment and serial numbers', CONVERT(varbinary(16), NEWID())),
                    (NEWID(), '{{TemplateId}}', 2, N'Complete installation, inspection or maintenance work', CONVERT(varbinary(16), NEWID())),
                    (NEWID(), '{{TemplateId}}', 3, N'Record test results and outstanding issues', CONVERT(varbinary(16), NEWID())),
                    (NEWID(), '{{TemplateId}}', 4, N'Capture photographic evidence', CONVERT(varbinary(16), NEWID())),
                    (NEWID(), '{{TemplateId}}', 5, N'Obtain technician and client sign-off', CONVERT(varbinary(16), NEWID()));
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Never remove a template already in use by real job cards.
        migrationBuilder.Sql($$"""
            IF EXISTS (
                SELECT 1 FROM [dbo].[JobTemplates]
                WHERE [Id] = '{{TemplateId}}' AND [Name] = N'Standard Job Card'
            )
            BEGIN
                IF EXISTS (SELECT 1 FROM [dbo].[JobCards] WHERE [JobTemplateId] = '{{TemplateId}}')
                    THROW 51211, 'Cannot remove the Standard Job Card template while jobs use it.', 1;

                DELETE FROM [dbo].[TemplateChecklistItems] WHERE [JobTemplateId] = '{{TemplateId}}';
                DELETE FROM [dbo].[JobTemplates] WHERE [Id] = '{{TemplateId}}';
            END;
            """);
    }
}
