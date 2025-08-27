using System;
using System.Data.Entity.Infrastructure.Annotations;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlterSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Schedules");

            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {   // identification fields (2)
                    ScheduleId = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    // description  (3)
                    EventName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EventNote = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    GroupName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    // type & status (2)
                    EventType = table.Column<int>(type: "int", nullable: true),
                    EventStatus = table.Column<int>(type: "int", nullable: true),
                    // Event Dates (2)
                    EventDateScheduled = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventDateCompleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    // Event Resources (3)
                    VehiclesDeliveryMax = table.Column<int>(type: "int", nullable: true),
                    VehiclesNormalMax = table.Column<int>(type: "int", nullable: true),
                    VolunteersMax = table.Column<int>(type: "int", nullable: true),

                    // system fields (5)
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdateUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                  constraints: table =>
                  {
                      table.PrimaryKey("PK_SChedules", x => x.ScheduleId);
                      table.ForeignKey(
                          name: "FK_Schedules_Locations_LocationId",
                          column: x => x.LocationId,
                          principalTable: "Locations",
                          principalColumn: "LocationId",
                          onDelete: ReferentialAction.Cascade);
                  }
                  
                  
                  );

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_LocationId",
                table: "Schedules",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_EventType",
                table: "Schedules",
                column: "EventType");

        }
    
    }
}
