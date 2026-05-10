using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBedRequestIdToEmailAndSmsQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BedRequestId",
                table: "SmsQueue",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BedRequestId",
                table: "EmailQueue",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SmsQueue_BedRequestId",
                table: "SmsQueue",
                column: "BedRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueue_BedRequestId",
                table: "EmailQueue",
                column: "BedRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailQueue_BedRequests_BedRequestId",
                table: "EmailQueue",
                column: "BedRequestId",
                principalTable: "BedRequests",
                principalColumn: "BedRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_SmsQueue_BedRequests_BedRequestId",
                table: "SmsQueue",
                column: "BedRequestId",
                principalTable: "BedRequests",
                principalColumn: "BedRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailQueue_BedRequests_BedRequestId",
                table: "EmailQueue");

            migrationBuilder.DropForeignKey(
                name: "FK_SmsQueue_BedRequests_BedRequestId",
                table: "SmsQueue");

            migrationBuilder.DropIndex(
                name: "IX_SmsQueue_BedRequestId",
                table: "SmsQueue");

            migrationBuilder.DropIndex(
                name: "IX_EmailQueue_BedRequestId",
                table: "EmailQueue");

            migrationBuilder.DropColumn(
                name: "BedRequestId",
                table: "SmsQueue");

            migrationBuilder.DropColumn(
                name: "BedRequestId",
                table: "EmailQueue");
        }
    }
}
