using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeScheduler.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "Schedules",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "Schedules",
                type: "TEXT",
                maxLength: 16,
                nullable: false,
                defaultValue: "Event");

            migrationBuilder.CreateTable(
                name: "OccurrenceCompletions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScheduleEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OccurrenceCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OccurrenceCompletions_Schedules_ScheduleEntryId",
                        column: x => x.ScheduleEntryId,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OccurrenceCompletions_ScheduleEntryId_Date",
                table: "OccurrenceCompletions",
                columns: new[] { "ScheduleEntryId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OccurrenceCompletions");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Schedules");
        }
    }
}
