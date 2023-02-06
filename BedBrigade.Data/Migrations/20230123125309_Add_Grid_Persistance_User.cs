using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGridPersistanceUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PersistConfig",
                table: "Users",
                type: "nvarchar(max)",
                maxLength: 5120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersistDonation",
                table: "Users",
                type: "nvarchar(max)",
                maxLength: 5120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersistLocation",
                table: "Users",
                type: "nvarchar(max)",
                maxLength: 5120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersistPages",
                table: "Users",
                type: "nvarchar(max)",
                maxLength: 5120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersistUser",
                table: "Users",
                type: "nvarchar(max)",
                maxLength: 5120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersistVolunteers",
                table: "Users",
                type: "nvarchar(max)",
                maxLength: 5120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersistConfig",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PersistDonation",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PersistLocation",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PersistPages",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PersistUser",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PersistVolunteers",
                table: "Users");
        }
    }
}
