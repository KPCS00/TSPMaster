using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TSPMaster.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisResultExtraFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HistoricalSeasonalitySummary",
                table: "AnalysisResults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IntradayMarketSnapshotJson",
                table: "AnalysisResults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TomorrowAllocationJson",
                table: "AnalysisResults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TomorrowEffectiveDate",
                table: "AnalysisResults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HistoricalSeasonalitySummary",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "IntradayMarketSnapshotJson",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "TomorrowAllocationJson",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "TomorrowEffectiveDate",
                table: "AnalysisResults");
        }
    }
}
