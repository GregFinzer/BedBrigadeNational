using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBedRequestScheduleId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ScheduleId",
                table: "BedRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BedRequests_ScheduleId",
                table: "BedRequests",
                column: "ScheduleId");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BedRequests_ScheduleId",
                table: "BedRequests");

            migrationBuilder.DropColumn(
                name: "ScheduleId",
                table: "BedRequests");
        }
    }
}
