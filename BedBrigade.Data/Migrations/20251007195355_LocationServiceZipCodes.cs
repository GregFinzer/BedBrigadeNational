using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class LocationServiceZipCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ServiceZipCodes",
                table: "Locations",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServiceZipCodes",
                table: "Locations");
        }
    }
}
