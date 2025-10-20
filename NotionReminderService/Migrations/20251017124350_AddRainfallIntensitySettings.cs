using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotionReminderService.Migrations
{
    /// <inheritdoc />
    public partial class AddRainfallIntensitySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RainfallIntensities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    LowerBound = table.Column<double>(type: "double precision", nullable: false),
                    UpperBound = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RainfallIntensities", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RainfallIntensities");
        }
    }
}
