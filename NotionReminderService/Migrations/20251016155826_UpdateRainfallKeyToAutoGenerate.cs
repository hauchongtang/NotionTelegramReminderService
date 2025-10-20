using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotionReminderService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRainfallKeyToAutoGenerate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RainfallId",
                table: "RainfallSlots",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RainfallId",
                table: "RainfallSlots",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
