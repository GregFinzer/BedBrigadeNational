using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExternalContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalContactUs",
                table: "Locations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExternalContent",
                table: "Locations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ExternalDonate",
                table: "Locations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalHome",
                table: "Locations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalRequestABed",
                table: "Locations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalVolunteer",
                table: "Locations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalContactUs",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "ExternalContent",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "ExternalDonate",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "ExternalHome",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "ExternalRequestABed",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "ExternalVolunteer",
                table: "Locations");
        }
    }
}
