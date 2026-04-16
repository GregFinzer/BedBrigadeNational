using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSignUpIdToEmailQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SignUpId",
                table: "EmailQueue",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueue_SignUpId",
                table: "EmailQueue",
                column: "SignUpId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailQueue_SignUps_SignUpId",
                table: "EmailQueue",
                column: "SignUpId",
                principalTable: "SignUps",
                principalColumn: "SignUpId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailQueue_SignUps_SignUpId",
                table: "EmailQueue");

            migrationBuilder.DropIndex(
                name: "IX_EmailQueue_SignUpId",
                table: "EmailQueue");

            migrationBuilder.DropColumn(
                name: "SignUpId",
                table: "EmailQueue");
        }
    }
}
