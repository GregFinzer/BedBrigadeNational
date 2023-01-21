using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Server.Migrations
{
    /// <inheritdoc />
    public partial class ChangeUserSalt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropColumn(
            name: "PasswordHash",
            table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordSalt",
                table: "Users");

            migrationBuilder.AddColumn<string>(
            name: "PasswordHash",
            table: "Users",
            type: "varbinary(255)",
            maxLength: 255,
            nullable: false,
            defaultValue: null);

            migrationBuilder.AddColumn<string>(
            name: "PasswordSalt",
            table: "Users",
            type: "varbinary(255)",
            maxLength: 255,
            nullable: false,
            defaultValue: null);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
            name: "PasswordSalt",
            table: "Users",
            type: "nvarchar(255)",
            maxLength: 255,
            nullable: false,
            defaultValue: "");

            migrationBuilder.AlterColumn<string>(
            name: "PasswordHash",
            table: "Users",
            type: "nvarchar(255)",
            maxLength: 255,
            nullable: false,
            defaultValue: "");
        }
    }
}
