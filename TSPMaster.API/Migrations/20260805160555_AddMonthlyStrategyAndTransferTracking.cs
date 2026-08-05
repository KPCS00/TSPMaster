using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TSPMaster.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyStrategyAndTransferTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastTransferMonth",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MonthlyTransfersCount",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MacroNewsSummary",
                table: "AnalysisResults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Move1PlanJson",
                table: "AnalysisResults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Move2PlanJson",
                table: "AnalysisResults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Move3PlanJson",
                table: "AnalysisResults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TargetMonth",
                table: "AnalysisResults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastTransferMonth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MonthlyTransfersCount",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MacroNewsSummary",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "Move1PlanJson",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "Move2PlanJson",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "Move3PlanJson",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "TargetMonth",
                table: "AnalysisResults");
        }
    }
}
