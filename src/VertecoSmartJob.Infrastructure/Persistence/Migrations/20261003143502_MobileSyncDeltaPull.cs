using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VertecoSmartJob.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MobileSyncDeltaPull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MobileSyncChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MobileSyncChanges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MobileSyncChanges_ChangedAtUtc",
                table: "MobileSyncChanges",
                column: "ChangedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MobileSyncChanges_JobCardId",
                table: "MobileSyncChanges",
                column: "JobCardId");

            migrationBuilder.CreateIndex(
                name: "IX_MobileSyncChanges_JobCardId_ChangedAtUtc",
                table: "MobileSyncChanges",
                columns: new[] { "JobCardId", "ChangedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MobileSyncChanges");
        }
    }
}
