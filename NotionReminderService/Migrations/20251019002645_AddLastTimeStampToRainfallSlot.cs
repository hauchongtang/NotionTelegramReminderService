using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotionReminderService.Migrations
{
    /// <inheritdoc />
    public partial class AddLastTimeStampToRainfallSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastTimeStamp",
                table: "RainfallSlots",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastTimeStamp",
                table: "RainfallSlots");
        }
    }
}
