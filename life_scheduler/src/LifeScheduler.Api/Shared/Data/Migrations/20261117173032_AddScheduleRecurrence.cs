using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeScheduler.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleRecurrence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Recurrence",
                table: "Schedules",
                type: "TEXT",
                maxLength: 16,
                nullable: false,
                defaultValue: "None");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Recurrence",
                table: "Schedules");
        }
    }
}
