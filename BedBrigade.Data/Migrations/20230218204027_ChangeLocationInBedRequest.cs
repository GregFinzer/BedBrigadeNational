using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeLocationInBedRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VolunteerForId",
                table: "Volunteers",
                newName: "VolunteeringForId");

            migrationBuilder.RenameIndex(
                name: "IX_Volunteers_VolunteerForId",
                table: "Volunteers",
                newName: "IX_Volunteers_VolunteeringForId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VolunteeringForId",
                table: "Volunteers",
                newName: "VolunteerForId");

            migrationBuilder.RenameIndex(
                name: "IX_Volunteers_VolunteeringForId",
                table: "Volunteers",
                newName: "IX_Volunteers_VolunteerForId");
        }
    }
}
