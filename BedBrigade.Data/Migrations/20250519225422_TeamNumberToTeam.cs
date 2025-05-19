using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class TeamNumberToTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeamNumber",
                table: "BedRequests");

            migrationBuilder.AddColumn<string>(
                name: "Team",
                table: "BedRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Team",
                table: "BedRequests");

            migrationBuilder.AddColumn<int>(
                name: "TeamNumber",
                table: "BedRequests",
                type: "int",
                nullable: true);
        }
    }
}
