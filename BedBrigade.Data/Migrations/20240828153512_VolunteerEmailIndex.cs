using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class VolunteerEmailIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Volunteers_Email",
                table: "Volunteers",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Volunteers_Email",
                table: "Volunteers");
        }
    }
}
