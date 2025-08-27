using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class VolunteerRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Volunteers_VolunteeringForId",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "IHaveAMinivan",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "IHaveAPickupTruck",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "IHaveAnSUV",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "VolunteeringForDate",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "VolunteeringForId",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "VehiclesDeliveryMax",
                table: "Schedules");

            migrationBuilder.RenameColumn(
                name: "VehiclesNormalMax",
                table: "Schedules",
                newName: "EventDurationHours");

            migrationBuilder.RenameColumn(
                name: "VehiclesDeliveryRegistered",
                table: "Schedules",
                newName: "DeliveryVehiclesRegistered");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EventDurationHours",
                table: "Schedules",
                newName: "VehiclesNormalMax");

            migrationBuilder.RenameColumn(
                name: "DeliveryVehiclesRegistered",
                table: "Schedules",
                newName: "VehiclesDeliveryRegistered");

            migrationBuilder.AddColumn<bool>(
                name: "IHaveAMinivan",
                table: "Volunteers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IHaveAPickupTruck",
                table: "Volunteers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IHaveAnSUV",
                table: "Volunteers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VolunteeringForDate",
                table: "Volunteers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "VolunteeringForId",
                table: "Volunteers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VehiclesDeliveryMax",
                table: "Schedules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Volunteers_VolunteeringForId",
                table: "Volunteers",
                column: "VolunteeringForId");
        }
    }
}
