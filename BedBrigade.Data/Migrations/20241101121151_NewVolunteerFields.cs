using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewVolunteerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AttendChurch",
                table: "Volunteers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OtherLanguagesSpoken",
                table: "Volunteers",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SubscribedEmail",
                table: "Volunteers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "SubscribedSms",
                table: "Volunteers",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttendChurch",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "OtherLanguagesSpoken",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "SubscribedEmail",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "SubscribedSms",
                table: "Volunteers");
        }
    }
}
