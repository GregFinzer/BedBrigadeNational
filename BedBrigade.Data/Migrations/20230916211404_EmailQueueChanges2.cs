using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class EmailQueueChanges2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDate",
                table: "EmailQueue",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreateUser",
                table: "EmailQueue",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MachineName",
                table: "EmailQueue",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdateUser",
                table: "EmailQueue",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreateDate",
                table: "EmailQueue");

            migrationBuilder.DropColumn(
                name: "CreateUser",
                table: "EmailQueue");

            migrationBuilder.DropColumn(
                name: "MachineName",
                table: "EmailQueue");

            migrationBuilder.DropColumn(
                name: "UpdateUser",
                table: "EmailQueue");
        }
    }
}
