using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IMS.Modular.Modules.InventoryIssues.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReporterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssigneeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AffectedQuantity = table.Column<int>(type: "INTEGER", nullable: true),
                    EstimatedLoss = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryIssues", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryIssues_AssigneeId",
                table: "InventoryIssues",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryIssues_CreatedAt",
                table: "InventoryIssues",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryIssues_DueDate",
                table: "InventoryIssues",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryIssues_LocationId",
                table: "InventoryIssues",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryIssues_Priority",
                table: "InventoryIssues",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryIssues_ProductId",
                table: "InventoryIssues",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryIssues_ReporterId",
                table: "InventoryIssues",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryIssues_Status",
                table: "InventoryIssues",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryIssues_Type",
                table: "InventoryIssues",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryIssues");
        }
    }
}
