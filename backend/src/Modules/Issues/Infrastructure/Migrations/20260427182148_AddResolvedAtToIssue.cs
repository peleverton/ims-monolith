using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IMS.Modular.Modules.Issues.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResolvedAtToIssue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "Issues",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "Issues");
        }
    }
}
