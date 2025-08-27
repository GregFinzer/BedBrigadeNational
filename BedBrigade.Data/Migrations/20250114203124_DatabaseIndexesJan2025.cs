using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseIndexesJan2025 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Translations_Culture",
                table: "Translations",
                column: "Culture");

            migrationBuilder.CreateIndex(
                name: "IX_SignUps_LocationId",
                table: "SignUps",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SignUps_ScheduleId",
                table: "SignUps",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTranslations_Culture",
                table: "ContentTranslations",
                column: "Culture");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTranslations_LocationId",
                table: "ContentTranslations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTranslations_Name",
                table: "ContentTranslations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Content_Name",
                table: "Content",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_BedRequests_Status",
                table: "BedRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Translations_Culture",
                table: "Translations");

            migrationBuilder.DropIndex(
                name: "IX_SignUps_LocationId",
                table: "SignUps");

            migrationBuilder.DropIndex(
                name: "IX_SignUps_ScheduleId",
                table: "SignUps");

            migrationBuilder.DropIndex(
                name: "IX_ContentTranslations_Culture",
                table: "ContentTranslations");

            migrationBuilder.DropIndex(
                name: "IX_ContentTranslations_LocationId",
                table: "ContentTranslations");

            migrationBuilder.DropIndex(
                name: "IX_ContentTranslations_Name",
                table: "ContentTranslations");

            migrationBuilder.DropIndex(
                name: "IX_Content_Name",
                table: "Content");

            migrationBuilder.DropIndex(
                name: "IX_BedRequests_Status",
                table: "BedRequests");
        }
    }
}
