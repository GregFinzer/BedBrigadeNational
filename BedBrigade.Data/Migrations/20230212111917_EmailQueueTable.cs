using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class EmailQueueTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "EmailQueue",
                columns: table => new
                {
                    EmailQueueId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromDisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FromAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ToAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 100, nullable: false),
                    HtmlBody = table.Column<string>(type: "nvarchar(4000)", maxLength: 100, nullable: false),
                    QueueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LockDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FailureMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailQueue", x => x.EmailQueueId);
                });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
             migrationBuilder.DropTable(
                name: "EmailQueue");

         }
    }
}
