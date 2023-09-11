using System;
using System.Data.Entity.Infrastructure.Annotations;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateVolunteerEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           // migrationBuilder.DropTable(
           //     name: "VolunteerEvents");

            migrationBuilder.CreateTable(
                name: "VolunteerEvents",
                columns: table => new
                {   // identification fields (4)
                    RegistrationId = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    ScheduleId = table.Column<int>(type: "int", nullable: false),
                    VolunteerId = table.Column<int>(type: "int", nullable: false),
                    // description  (1)                    
                    VolunteerEventNote = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                  
                    // system fields (5)
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdateUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                  constraints: table =>
                  {
                      table.PrimaryKey("PK_VolunteerEvents", x => x.RegistrationId);
                      table.ForeignKey(
                          name: "FK_VolunteerEvents_Locations_LocationId",
                          column: x => x.LocationId,
                          principalTable: "Locations",
                          principalColumn: "LocationId",
                          onDelete: ReferentialAction.NoAction);
                     // table.ForeignKey(
                     //       name: "FK_VolunteerEvents_Schedules_ScheduleId",
                      //      column: x => x.ScheduleId,
                      //      principalTable: "Schedules",
                       //     principalColumn: "ScheduleId",
                      //      onDelete: ReferentialAction.NoAction);
                      table.ForeignKey(
                              name: "FK_VolunteerEvents_Volunteers_VolunteerId",
                              column: x => x.VolunteerId,
                              principalTable: "Volunteers",
                              principalColumn: "VolunteerId",
                              onDelete: ReferentialAction.NoAction);
                  }                  
                  
                  );

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerEvents_LocationId",
                table: "VolunteerEvents",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerEvents_ScheduleId",
                table: "VolunteerEvents",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                 name: "IX_VolunteerEvents_VolunteerId",
                 table: "VolunteerEvents",
                 column: "VolunteerId");

        }
    
    }
}
