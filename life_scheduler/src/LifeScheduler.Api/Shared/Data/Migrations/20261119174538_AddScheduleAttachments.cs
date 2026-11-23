using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeScheduler.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduleAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScheduleEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 127, nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    StorageKey = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false, defaultValue: "Pending"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleAttachments_Schedules_ScheduleEntryId",
                        column: x => x.ScheduleEntryId,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleAttachments_ScheduleEntryId",
                table: "ScheduleAttachments",
                column: "ScheduleEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleAttachments_StorageKey",
                table: "ScheduleAttachments",
                column: "StorageKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleAttachments");
        }
    }
}
