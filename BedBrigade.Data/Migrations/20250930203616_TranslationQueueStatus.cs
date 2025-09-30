using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class TranslationQueueStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailureMessage",
                table: "TranslationQueue",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockDate",
                table: "TranslationQueue",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QueueDate",
                table: "TranslationQueue",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "SentDate",
                table: "TranslationQueue",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "TranslationQueue",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FailureMessage",
                table: "ContentTranslationQueue",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockDate",
                table: "ContentTranslationQueue",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QueueDate",
                table: "ContentTranslationQueue",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "SentDate",
                table: "ContentTranslationQueue",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ContentTranslationQueue",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_GeoLocation_TableId_TableName",
                table: "GeoLocationQueue",
                columns: new[] { "TableId", "TableName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GeoLocation_TableId_TableName",
                table: "GeoLocationQueue");

            migrationBuilder.DropColumn(
                name: "FailureMessage",
                table: "TranslationQueue");

            migrationBuilder.DropColumn(
                name: "LockDate",
                table: "TranslationQueue");

            migrationBuilder.DropColumn(
                name: "QueueDate",
                table: "TranslationQueue");

            migrationBuilder.DropColumn(
                name: "SentDate",
                table: "TranslationQueue");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TranslationQueue");

            migrationBuilder.DropColumn(
                name: "FailureMessage",
                table: "ContentTranslationQueue");

            migrationBuilder.DropColumn(
                name: "LockDate",
                table: "ContentTranslationQueue");

            migrationBuilder.DropColumn(
                name: "QueueDate",
                table: "ContentTranslationQueue");

            migrationBuilder.DropColumn(
                name: "SentDate",
                table: "ContentTranslationQueue");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ContentTranslationQueue");
        }
    }
}
