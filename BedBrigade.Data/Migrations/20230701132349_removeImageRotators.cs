using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class removeImageRotators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FooterMediaId",
                table: "Content");

            migrationBuilder.DropColumn(
                name: "HeaderMediaId",
                table: "Content");

            migrationBuilder.DropColumn(
                name: "LeftMediaId",
                table: "Content");

            migrationBuilder.DropColumn(
                name: "MiddleMediaId",
                table: "Content");

            migrationBuilder.DropColumn(
                name: "RightMediaId",
                table: "Content");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FooterMediaId",
                table: "Content",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeaderMediaId",
                table: "Content",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeftMediaId",
                table: "Content",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MiddleMediaId",
                table: "Content",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RightMediaId",
                table: "Content",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
