using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VertecoSmartJob.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MobileSyncOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MobileSyncOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TechnicianId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MobileSyncOperations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MobileSyncOperations_JobCardId",
                table: "MobileSyncOperations",
                column: "JobCardId");

            migrationBuilder.CreateIndex(
                name: "IX_MobileSyncOperations_ProcessedAtUtc",
                table: "MobileSyncOperations",
                column: "ProcessedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MobileSyncOperations_TechnicianId_OperationId",
                table: "MobileSyncOperations",
                columns: new[] { "TechnicianId", "OperationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MobileSyncOperations");
        }
    }
}
