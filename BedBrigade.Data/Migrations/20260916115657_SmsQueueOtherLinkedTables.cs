using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class SmsQueueOtherLinkedTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ContactUsId",
                table: "SmsQueue",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VolunteerId",
                table: "SmsQueue",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SmsQueue_ContactUsId",
                table: "SmsQueue",
                column: "ContactUsId");

            migrationBuilder.CreateIndex(
                name: "IX_SmsQueue_VolunteerId",
                table: "SmsQueue",
                column: "VolunteerId");

            migrationBuilder.AddForeignKey(
                name: "FK_SmsQueue_ContactUs_ContactUsId",
                table: "SmsQueue",
                column: "ContactUsId",
                principalTable: "ContactUs",
                principalColumn: "ContactUsId");

            migrationBuilder.AddForeignKey(
                name: "FK_SmsQueue_Volunteers_VolunteerId",
                table: "SmsQueue",
                column: "VolunteerId",
                principalTable: "Volunteers",
                principalColumn: "VolunteerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SmsQueue_ContactUs_ContactUsId",
                table: "SmsQueue");

            migrationBuilder.DropForeignKey(
                name: "FK_SmsQueue_Volunteers_VolunteerId",
                table: "SmsQueue");

            migrationBuilder.DropIndex(
                name: "IX_SmsQueue_ContactUsId",
                table: "SmsQueue");

            migrationBuilder.DropIndex(
                name: "IX_SmsQueue_VolunteerId",
                table: "SmsQueue");

            migrationBuilder.DropColumn(
                name: "ContactUsId",
                table: "SmsQueue");

            migrationBuilder.DropColumn(
                name: "VolunteerId",
                table: "SmsQueue");
        }
    }
}
