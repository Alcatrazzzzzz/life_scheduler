using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeScheduler.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHabitStackingAndReward : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reward",
                table: "Schedules",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StackAfterId",
                table: "Schedules",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_StackAfterId",
                table: "Schedules",
                column: "StackAfterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Schedules_StackAfterId",
                table: "Schedules",
                column: "StackAfterId",
                principalTable: "Schedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Schedules_StackAfterId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_StackAfterId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "Reward",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "StackAfterId",
                table: "Schedules");
        }
    }
}
