using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeScheduler.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeScheduleTimeOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeOnly>(
                name: "Start",
                table: "Schedules",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(TimeOnly),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "End",
                table: "Schedules",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(TimeOnly),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeOnly>(
                name: "Start",
                table: "Schedules",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0),
                oldClrType: typeof(TimeOnly),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "End",
                table: "Schedules",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0),
                oldClrType: typeof(TimeOnly),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
