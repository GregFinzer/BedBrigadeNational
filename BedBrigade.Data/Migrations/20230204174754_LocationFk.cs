using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class LocationFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Locations_LocationId",
                table: "Users");

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

            migrationBuilder.RenameColumn(
                name: "LocationId",
                table: "Users",
                newName: "FkLocation");

            migrationBuilder.RenameIndex(
                name: "IX_Users_LocationId",
                table: "Users",
                newName: "IX_Users_FkLocation");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Schedules",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<int>(
                name: "RightMediaId",
                table: "Content",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MiddleMediaId",
                table: "Content",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MainMediaId",
                table: "Content",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LeftMediaId",
                table: "Content",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Locations_FkLocation",
                table: "Users",
                column: "FkLocation",
                principalTable: "Locations",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Locations_FkLocation",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "FkLocation",
                table: "Users",
                newName: "LocationId");

            migrationBuilder.RenameIndex(
                name: "IX_Users_FkLocation",
                table: "Users",
                newName: "IX_Users_LocationId");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

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

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Schedules",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RightMediaId",
                table: "Content",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "MiddleMediaId",
                table: "Content",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "MainMediaId",
                table: "Content",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "LeftMediaId",
                table: "Content",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Locations_LocationId",
                table: "Users",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
