using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotionReminderService.Migrations
{
    /// <inheritdoc />
    public partial class AddRainfallAndRainfallSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Rainfalls",
                columns: table => new
                {
                    RainfallId = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SlotsPerHour = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rainfalls", x => x.RainfallId);
                });

            migrationBuilder.CreateTable(
                name: "RainfallSlots",
                columns: table => new
                {
                    RainfallSlotId = table.Column<string>(type: "text", nullable: false),
                    RainfallId = table.Column<string>(type: "text", nullable: false),
                    StationId = table.Column<string>(type: "text", nullable: false),
                    HourOfDay = table.Column<int>(type: "integer", nullable: false),
                    SlotNumber = table.Column<int>(type: "integer", nullable: false),
                    RainfallAmount = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RainfallSlots", x => x.RainfallSlotId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rainfalls");

            migrationBuilder.DropTable(
                name: "RainfallSlots");
        }
    }
}
