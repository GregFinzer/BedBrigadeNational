using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserPersist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersistBedRequest",
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
                name: "PersistMedia",
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

            migrationBuilder.CreateTable(
                name: "UserPersist",
                columns: table => new
                {
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Grid = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdateUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPersist", x => new { x.UserName, x.Grid });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPersist");

            migrationBuilder.AddColumn<string>(
                name: "PersistBedRequest",
                table: "Users",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PersistConfig",
                table: "Users",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PersistDonation",
                table: "Users",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PersistLocation",
                table: "Users",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PersistMedia",
                table: "Users",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PersistPages",
                table: "Users",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PersistUser",
                table: "Users",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PersistVolunteers",
                table: "Users",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");
        }
    }
}
