using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class PolarisScheduleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address1",
                table: "Schedules",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address2",
                table: "Schedules",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Schedules",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizerEmail",
                table: "Schedules",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizerName",
                table: "Schedules",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizerPhone",
                table: "Schedules",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Schedules",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Schedules",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address1",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "Address2",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "OrganizerEmail",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "OrganizerName",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "OrganizerPhone",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Schedules");
        }
    }
}
