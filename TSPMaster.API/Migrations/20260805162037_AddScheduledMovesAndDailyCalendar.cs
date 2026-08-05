using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TSPMaster.API.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledMovesAndDailyCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DailyCalendarJson",
                table: "AnalysisResults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ScheduledMovesJson",
                table: "AnalysisResults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyCalendarJson",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "ScheduledMovesJson",
                table: "AnalysisResults");
        }
    }
}
