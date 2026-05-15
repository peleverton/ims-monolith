using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IMS.Modular.Modules.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeleteRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeleteRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ScheduledHardDeleteAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CancelledByAdminId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeleteRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeleteRequests_UserId",
                table: "DeleteRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeleteRequests_Status_ScheduledHardDeleteAt",
                table: "DeleteRequests",
                columns: new[] { "Status", "ScheduledHardDeleteAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeleteRequests");
        }
    }
}
