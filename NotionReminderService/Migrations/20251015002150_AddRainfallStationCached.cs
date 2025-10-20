using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotionReminderService.Migrations
{
    /// <inheritdoc />
    public partial class AddRainfallStationCached : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RainfallStations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DeviceId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RainfallStations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RainfallStations");
        }
    }
}
