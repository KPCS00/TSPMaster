using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TSPMaster.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAllocationMovesAndBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleId",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentTspBalance",
                table: "AspNetUsers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "InitialBalanceDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InitialTspBalance",
                table: "AspNetUsers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "AllocationMoves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    BalanceAtMove = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocationsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MoveNumberInMonth = table.Column<int>(type: "int", nullable: false),
                    MonthKey = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocationMoves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllocationMoves_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationMove_User_Date",
                table: "AllocationMoves",
                columns: new[] { "UserId", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationMove_User_Month",
                table: "AllocationMoves",
                columns: new[] { "UserId", "MonthKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllocationMoves");

            migrationBuilder.DropColumn(
                name: "CurrentTspBalance",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "InitialBalanceDate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "InitialTspBalance",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "GoogleId",
                table: "AspNetUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
