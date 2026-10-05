using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace VertecoSmartJob.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261005120000_RetainReopenedFieldWork")]
public sealed class RetainReopenedFieldWork : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("FieldEquipmentName", "JobCards", type: "nvarchar(200)", maxLength: 200, nullable: true);
        migrationBuilder.AddColumn<string>("FieldEquipmentSerialNumber", "JobCards", type: "nvarchar(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>("FieldEquipmentDescription", "JobCards", type: "nvarchar(2000)", maxLength: 2000, nullable: true);
        migrationBuilder.AddColumn<string>("TechnicianComments", "JobCards", type: "nvarchar(2000)", maxLength: 2000, nullable: true);

        // Older captures wrote equipment to the site and recorded its serial in the job audit.
        // Recover only a definite match; never guess when a site has several assets.
        migrationBuilder.Sql("""
            UPDATE j
            SET j.FieldEquipmentName = captured.Name,
                j.FieldEquipmentSerialNumber = captured.SerialNumber,
                j.FieldEquipmentDescription = captured.Description
            FROM dbo.JobCards AS j
            CROSS APPLY (
                SELECT TOP (1) e.Name, e.SerialNumber, e.Description
                FROM dbo.AuditLogs AS a
                INNER JOIN dbo.Equipment AS e
                    ON e.SiteId = j.SiteId AND e.SerialNumber = a.Details
                WHERE a.JobCardId = j.Id AND a.Action = N'EquipmentCaptured'
                ORDER BY a.CreatedAtUtc DESC, a.Id DESC
            ) AS captured;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("FieldEquipmentName", "JobCards");
        migrationBuilder.DropColumn("FieldEquipmentSerialNumber", "JobCards");
        migrationBuilder.DropColumn("FieldEquipmentDescription", "JobCards");
        migrationBuilder.DropColumn("TechnicianComments", "JobCards");
    }
}
