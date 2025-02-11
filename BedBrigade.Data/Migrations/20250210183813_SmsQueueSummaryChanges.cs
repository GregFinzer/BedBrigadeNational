using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class SmsQueueSummaryChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ToPhoneNumber",
                table: "SmsQueue",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "FromPhoneNumber",
                table: "SmsQueue",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "SmsQueue",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactType",
                table: "SmsQueue",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SmsQueue_LocationId",
                table: "SmsQueue",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SmsQueue_ToPhoneNumber",
                table: "SmsQueue",
                column: "ToPhoneNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SmsQueue_LocationId",
                table: "SmsQueue");

            migrationBuilder.DropIndex(
                name: "IX_SmsQueue_ToPhoneNumber",
                table: "SmsQueue");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "SmsQueue");

            migrationBuilder.DropColumn(
                name: "ContactType",
                table: "SmsQueue");

            migrationBuilder.AlterColumn<string>(
                name: "ToPhoneNumber",
                table: "SmsQueue",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(14)",
                oldMaxLength: 14);

            migrationBuilder.AlterColumn<string>(
                name: "FromPhoneNumber",
                table: "SmsQueue",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(14)",
                oldMaxLength: 14);
        }
    }
}
