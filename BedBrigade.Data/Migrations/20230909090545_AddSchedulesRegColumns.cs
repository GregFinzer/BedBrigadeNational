using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulesRegColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VehiclesDeliveryRegistered",
                table: "Schedules",
                type: "int",
                nullable: true,
                defaultValue: 0);
            migrationBuilder.AddColumn<int>(
               name: "VolunteersRegistered",
               table: "Schedules",
               type: "int",
               nullable: true,
               defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VehiclesDeliveryRegistered",
                table: "Schedules",
                type: "int",
                nullable: true,
                defaultValue: 0);
            migrationBuilder.AddColumn<int>(
               name: "VolunteersRegistered",
               table: "Schedules",
               type: "int",
               nullable: true,
               defaultValue: 0);
        }
    }
}
