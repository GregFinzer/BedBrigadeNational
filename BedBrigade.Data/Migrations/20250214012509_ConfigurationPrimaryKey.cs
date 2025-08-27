using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConfigurationPrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Configurations",
                table: "Configurations");

            migrationBuilder.AddColumn<int>(
                name: "ConfigurationId",
                table: "Configurations",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Configurations",
                table: "Configurations",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_Configuration_LocationId_ConfigurationKey",
                table: "Configurations",
                columns: new[] { "LocationId", "ConfigurationKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Configurations",
                table: "Configurations");

            migrationBuilder.DropIndex(
                name: "IX_Configuration_LocationId_ConfigurationKey",
                table: "Configurations");

            migrationBuilder.DropColumn(
                name: "ConfigurationId",
                table: "Configurations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Configurations",
                table: "Configurations",
                columns: new[] { "ConfigurationKey", "LocationId" });
        }
    }
}
