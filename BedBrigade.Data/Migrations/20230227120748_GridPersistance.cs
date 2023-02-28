using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class GridPersistance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "PasswordSalt",
                table: "Users",
                type: "varbinary(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<byte[]>(
                name: "PasswordHash",
                table: "Users",
                type: "varbinary(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "PersistBedRequest",
                table: "Users",
                type: "nvarchar(4000)",
                nullable: false,
                defaultValue: "");


            migrationBuilder.AddColumn<string>(
                name: "PersistMedia",
                table: "Users",
                type: "nvarchar(4000)",
                nullable: false,
                defaultValue: "");


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersistBedRequest",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PersistMedia",
                table: "Users");


            migrationBuilder.AlterColumn<byte[]>(
                name: "PasswordSalt",
                table: "Users",
                type: "varbinary(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "varbinary(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "PasswordHash",
                table: "Users",
                type: "varbinary(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "varbinary(255)",
                oldMaxLength: 255,
                oldNullable: true);
        }
    }
}
