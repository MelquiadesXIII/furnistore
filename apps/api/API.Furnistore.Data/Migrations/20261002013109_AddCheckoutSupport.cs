using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Furnistore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "OrderDetails",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "OrderDetails" AS od
                SET "ProductName" = p."Name"
                FROM "Products" AS p
                WHERE p."Id" = od."ProductId";
                """);

            migrationBuilder.Sql("""
                DELETE FROM "CartItems" AS ci
                WHERE NOT EXISTS (SELECT 1 FROM "Clients" AS c WHERE c."ID" = ci."ClientId")
                   OR NOT EXISTS (SELECT 1 FROM "Products" AS p WHERE p."Id" = ci."ProductId");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductId",
                table: "CartItems",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_Clients_ClientId",
                table: "CartItems",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_Products_ProductId",
                table: "CartItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_Clients_ClientId",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_Products_ProductId",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_ProductId",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "OrderDetails");
        }
    }
}
