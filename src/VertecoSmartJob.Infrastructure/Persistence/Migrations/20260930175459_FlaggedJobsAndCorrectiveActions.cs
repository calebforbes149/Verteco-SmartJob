using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VertecoSmartJob.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FlaggedJobsAndCorrectiveActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FlagReason",
                table: "JobCards",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FlaggedAtUtc",
                table: "JobCards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFlagged",
                table: "JobCards",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CorrectiveActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OpenedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpenedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorrectiveActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorrectiveActions_JobCards_JobCardId",
                        column: x => x.JobCardId,
                        principalTable: "JobCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobCards_IsFlagged",
                table: "JobCards",
                column: "IsFlagged");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveActions_JobCardId_ResolvedAtUtc",
                table: "CorrectiveActions",
                columns: new[] { "JobCardId", "ResolvedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveActions_OpenedAtUtc",
                table: "CorrectiveActions",
                column: "OpenedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CorrectiveActions");

            migrationBuilder.DropIndex(
                name: "IX_JobCards_IsFlagged",
                table: "JobCards");

            migrationBuilder.DropColumn(
                name: "FlagReason",
                table: "JobCards");

            migrationBuilder.DropColumn(
                name: "FlaggedAtUtc",
                table: "JobCards");

            migrationBuilder.DropColumn(
                name: "IsFlagged",
                table: "JobCards");
        }
    }
}
