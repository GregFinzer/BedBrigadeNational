using BedBrigade.Common.Constants;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class EmailLocationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SignUps_Volunteers_VolunteerId",
                table: "SignUps");

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "EmailQueue",
                type: "int",
                nullable: false,
                defaultValue: Defaults.GroveCityLocationId);

            migrationBuilder.AddForeignKey(
                name: "FK_SignUps_Schedules_ScheduleId",
                table: "SignUps",
                column: "ScheduleId",
                principalTable: "Schedules",
                principalColumn: "ScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_SignUps_Volunteers_VolunteerId",
                table: "SignUps",
                column: "VolunteerId",
                principalTable: "Volunteers",
                principalColumn: "VolunteerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SignUps_Schedules_ScheduleId",
                table: "SignUps");

            migrationBuilder.DropForeignKey(
                name: "FK_SignUps_Volunteers_VolunteerId",
                table: "SignUps");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "EmailQueue");

            migrationBuilder.AddForeignKey(
                name: "FK_SignUps_Volunteers_VolunteerId",
                table: "SignUps",
                column: "VolunteerId",
                principalTable: "Volunteers",
                principalColumn: "VolunteerId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
