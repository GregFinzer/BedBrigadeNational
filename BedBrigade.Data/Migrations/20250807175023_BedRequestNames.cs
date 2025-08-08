using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class BedRequestNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AgesGender",
                table: "BedRequests",
                newName: "GenderAge");

            migrationBuilder.AlterColumn<string>(
                name: "BedType",
                table: "BedRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Names",
                table: "BedRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Names",
                table: "BedRequests");

            migrationBuilder.RenameColumn(
                name: "GenderAge",
                table: "BedRequests",
                newName: "AgesGender");

            migrationBuilder.AlterColumn<string>(
                name: "BedType",
                table: "BedRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
