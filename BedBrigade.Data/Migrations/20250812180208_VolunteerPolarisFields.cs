using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class VolunteerPolarisFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Volunteers_Email",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "NumberOfVolunteers",
                table: "Volunteers");

            migrationBuilder.RenameColumn(
                name: "OrganizationOrGroup",
                table: "Volunteers",
                newName: "Organization");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Volunteers",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(14)",
                oldMaxLength: 14);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Volunteers",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(25)",
                oldMaxLength: 25);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Volunteers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<int>(
                name: "CanYouTranslate",
                table: "Volunteers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChurchName",
                table: "Volunteers",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Group",
                table: "Volunteers",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherArea",
                table: "Volunteers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleDescription",
                table: "Volunteers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VolunteerArea",
                table: "Volunteers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Volunteers_Email",
                table: "Volunteers",
                column: "Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Volunteers_Email",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "CanYouTranslate",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "ChurchName",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "Group",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "OtherArea",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "VehicleDescription",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "VolunteerArea",
                table: "Volunteers");

            migrationBuilder.RenameColumn(
                name: "Organization",
                table: "Volunteers",
                newName: "OrganizationOrGroup");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Volunteers",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(14)",
                oldMaxLength: 14,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Volunteers",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(25)",
                oldMaxLength: 25,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Volunteers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfVolunteers",
                table: "Volunteers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Volunteers_Email",
                table: "Volunteers",
                column: "Email",
                unique: true);
        }
    }
}
