using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class MailingAddressBuildAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Address1",
                table: "Locations",
                newName: "BuildAddress");

            migrationBuilder.RenameColumn(
                name: "City",
                table: "Locations",
                newName: "BuildCity");

            migrationBuilder.RenameColumn(
                name: "State",
                table: "Locations",
                newName: "BuildState");

            migrationBuilder.RenameColumn(
                name: "PostalCode",
                table: "Locations",
                newName: "BuildPostalCode");

            migrationBuilder.AddColumn<string>(
                name: "MailingAddress",
                table: "Locations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MailingCity",
                table: "Locations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MailingState",
                table: "Locations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MailingPostalCode",
                table: "Locations",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the newly added columns
            migrationBuilder.DropColumn(
                name: "MailingAddress",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "MailingCity",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "MailingState",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "MailingPostalCode",
                table: "Locations");

            // Rename the columns back to their original names
            migrationBuilder.RenameColumn(
                name: "BuildAddress",
                table: "Locations",
                newName: "Address1");

            migrationBuilder.RenameColumn(
                name: "BuildCity",
                table: "Locations",
                newName: "City");

            migrationBuilder.RenameColumn(
                name: "BuildState",
                table: "Locations",
                newName: "State");

            migrationBuilder.RenameColumn(
                name: "BuildPostalCode",
                table: "Locations",
                newName: "PostalCode");
        }

    }
}
