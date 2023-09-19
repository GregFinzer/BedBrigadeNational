using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class VolunteerForMaintFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDate",
                table: "VolunteersFor",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreateUser",
                table: "VolunteersFor",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MachineName",
                table: "VolunteersFor",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "VolunteersFor",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdateUser",
                table: "VolunteersFor",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreateDate",
                table: "VolunteersFor");

            migrationBuilder.DropColumn(
                name: "CreateUser",
                table: "VolunteersFor");

            migrationBuilder.DropColumn(
                name: "MachineName",
                table: "VolunteersFor");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "VolunteersFor");

            migrationBuilder.DropColumn(
                name: "UpdateUser",
                table: "VolunteersFor");
        }
    }
}
