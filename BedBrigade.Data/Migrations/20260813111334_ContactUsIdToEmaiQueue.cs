using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class ContactUsIdToEmaiQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ContactUsId",
                table: "EmailQueue",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueue_ContactUsId",
                table: "EmailQueue",
                column: "ContactUsId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailQueue_SignUps_ContactUsId",
                table: "EmailQueue",
                column: "ContactUsId",
                principalTable: "SignUps",
                principalColumn: "SignUpId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailQueue_SignUps_ContactUsId",
                table: "EmailQueue");

            migrationBuilder.DropIndex(
                name: "IX_EmailQueue_ContactUsId",
                table: "EmailQueue");

            migrationBuilder.DropColumn(
                name: "ContactUsId",
                table: "EmailQueue");
        }
    }
}
