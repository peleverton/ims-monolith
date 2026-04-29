using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IMS.Modular.Modules.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIdToAllEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Products
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Products",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId",
                table: "Products",
                column: "TenantId");

            // Suppliers
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Suppliers",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TenantId",
                table: "Suppliers",
                column: "TenantId");

            // Locations
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Locations",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId",
                table: "Locations",
                column: "TenantId");

            // StockMovements
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "StockMovements",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId",
                table: "StockMovements",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Products_TenantId",      table: "Products");
            migrationBuilder.DropIndex(name: "IX_Suppliers_TenantId",     table: "Suppliers");
            migrationBuilder.DropIndex(name: "IX_Locations_TenantId",     table: "Locations");
            migrationBuilder.DropIndex(name: "IX_StockMovements_TenantId", table: "StockMovements");

            migrationBuilder.DropColumn(name: "TenantId", table: "Products");
            migrationBuilder.DropColumn(name: "TenantId", table: "Suppliers");
            migrationBuilder.DropColumn(name: "TenantId", table: "Locations");
            migrationBuilder.DropColumn(name: "TenantId", table: "StockMovements");
        }
    }
}
