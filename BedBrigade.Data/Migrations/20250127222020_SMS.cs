using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class SMS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Configurations",
                table: "Configurations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Configurations",
                table: "Configurations",
                columns: new[] { "ConfigurationKey", "LocationId" });

            migrationBuilder.CreateTable(
                name: "SmsQueue",
                columns: table => new
                {
                    SmsQueueId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromPhoneNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ToPhoneNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(1600)", maxLength: 1600, nullable: false),
                    QueueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LockDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FailureMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SignUpId = table.Column<int>(type: "int", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdateUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsQueue", x => x.SmsQueueId);
                    table.ForeignKey(
                        name: "FK_SmsQueue_SignUps_SignUpId",
                        column: x => x.SignUpId,
                        principalTable: "SignUps",
                        principalColumn: "SignUpId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SmsQueue_SignUpId",
                table: "SmsQueue",
                column: "SignUpId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SmsQueue");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Configurations",
                table: "Configurations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Configurations",
                table: "Configurations",
                column: "ConfigurationKey");
        }
    }
}
