using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNewFieldsToBedRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrimaryLanguage",
                table: "BedRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SpeakEnglish",
                table: "BedRequests",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrimaryLanguage",
                table: "BedRequests");

            migrationBuilder.DropColumn(
                name: "SpeakEnglish",
                table: "BedRequests");
        }
    }
}
