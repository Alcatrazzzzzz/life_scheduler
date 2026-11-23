using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeScheduler.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleArchiveState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAt",
                table: "Schedules",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Schedules",
                type: "TEXT",
                maxLength: 16,
                nullable: false,
                defaultValue: "Active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Schedules");
        }
    }
}
