using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Furnistore.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReshapeDomainModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "Clients" WHERE "UserId" IS NULL) THEN
                        RAISE EXCEPTION 'ReshapeDomainModel: hay clientes sin cuenta (UserId NULL). Asócialos a un usuario o elimínalos antes de migrar.';
                    END IF;
                    IF EXISTS (SELECT 1 FROM "Orders" o WHERE NOT EXISTS (SELECT 1 FROM "Clients" c WHERE c."ID" = o."ClientId")) THEN
                        RAISE EXCEPTION 'ReshapeDomainModel: hay órdenes de clientes inexistentes. Corrígelas antes de migrar.';
                    END IF;
                    IF EXISTS (SELECT 1 FROM "Products" p WHERE NOT EXISTS (SELECT 1 FROM "ProductCategories" c WHERE c."Id" = p."ProductCategoryId")) THEN
                        RAISE EXCEPTION 'ReshapeDomainModel: hay productos con una categoría inexistente. Corrígelos antes de migrar.';
                    END IF;
                    IF EXISTS (SELECT lower("Name") FROM "ProductCategories" GROUP BY 1 HAVING count(*) > 1) THEN
                        RAISE EXCEPTION 'ReshapeDomainModel: hay categorías con el mismo nombre. Unifícalas antes de migrar.';
                    END IF;
                    IF EXISTS (SELECT 1 FROM "Products" WHERE "Price" <= 0 OR "Stock" < 0) THEN
                        RAISE EXCEPTION 'ReshapeDomainModel: hay productos con precio no positivo o stock negativo. Corrígelos antes de migrar.';
                    END IF;
                    IF EXISTS (SELECT 1 FROM "OrderDetails" WHERE "Quantity" <= 0 OR "UnitPrice" < 0) THEN
                        RAISE EXCEPTION 'ReshapeDomainModel: hay líneas de órdenes con cantidad no positiva o precio negativo. Corrígelas antes de migrar.';
                    END IF;
                END $$;
                """
            );

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Products_ProductId",
                table: "OrderDetails");

            migrationBuilder.AddColumn<string>(
                name: "TokenHash",
                table: "RefreshTokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql(
                """
                DELETE FROM "RefreshTokens" r
                WHERE NOT EXISTS (SELECT 1 FROM "AspNetUsers" u WHERE u."Id" = r."UserId");

                UPDATE "RefreshTokens"
                SET "TokenHash" = encode(sha256(convert_to("Token", 'UTF8')), 'hex');
                """
            );

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "RefreshTokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Token",
                table: "RefreshTokens");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Products",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Products",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "WidthCm",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepthCm",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HeightCm",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Material",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.RenameColumn(
                name: "OrderDate",
                table: "Orders",
                newName: "PlacedAt");

            migrationBuilder.AlterColumn<decimal>(
                name: "Total",
                table: "Orders",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "Orders",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingCost",
                table: "Orders",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ShipToName",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShipToPhone",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShipToStreet",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShipToCity",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShipToProvince",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShipToDeliveryNotes",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EstimatedDeliveryDate",
                table: "Orders",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Orders"
                SET "Status" = CASE "Status"
                    WHEN '0' THEN 'Paid'
                    WHEN '1' THEN 'Paid'
                    WHEN '2' THEN 'Shipped'
                    WHEN '3' THEN 'Delivered'
                    WHEN '4' THEN 'Cancelled'
                END;

                UPDATE "Orders" o
                SET "Subtotal" = o."Total",
                    "ShippingCost" = 0,
                    "PaidAt" = o."PlacedAt",
                    "EstimatedDeliveryDate" = (o."DeliveryDate" AT TIME ZONE 'UTC')::date,
                    "ShippedAt" = CASE WHEN o."Status" IN ('Shipped', 'Delivered') THEN o."PlacedAt" END,
                    "DeliveredAt" = CASE WHEN o."Status" = 'Delivered' THEN o."DeliveryDate" END,
                    "CancelledAt" = CASE WHEN o."Status" = 'Cancelled' THEN o."PlacedAt" END,
                    "ShipToName" = c."FirstName" || ' ' || c."LastName",
                    "ShipToPhone" = COALESCE(NULLIF(c."Phone", '+10000000000'), ''),
                    "ShipToStreet" = COALESCE(NULLIF(btrim(c."Address"), 'Pendiente de completar'), '')
                FROM "Clients" c
                WHERE c."ID" = o."ClientId";
                """
            );

            migrationBuilder.DropColumn(
                name: "DeliveryDate",
                table: "Orders");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "OrderDetails",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Clients",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Clients",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "Clients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Clients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "Clients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryNotes",
                table: "Clients",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Clients"
                SET "Phone" = NULLIF("Phone", '+10000000000'),
                    "Street" = NULLIF(NULLIF(btrim("Address"), 'Pendiente de completar'), '');
                """
            );

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "Clients");

            migrationBuilder.Sql(
                """
                DELETE FROM "CartItems" WHERE "Quantity" <= 0;

                CREATE UNIQUE INDEX "UX_ProductCategories_Name_Lower" ON "ProductCategories" (lower("Name"));
                """
            );

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductCategoryId",
                table: "Products",
                column: "ProductCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ClientId_PlacedAt",
                table: "Orders",
                columns: new[] { "ClientId", "PlacedAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_Dimensions_Positive",
                table: "Products",
                sql: "(\"WidthCm\" IS NULL OR \"WidthCm\" > 0) AND (\"DepthCm\" IS NULL OR \"DepthCm\" > 0) AND (\"HeightCm\" IS NULL OR \"HeightCm\" > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_Price_Positive",
                table: "Products",
                sql: "\"Price\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_Stock_NonNegative",
                table: "Products",
                sql: "\"Stock\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Amounts",
                table: "Orders",
                sql: "\"Subtotal\" >= 0 AND \"ShippingCost\" >= 0 AND \"Total\" = \"Subtotal\" + \"ShippingCost\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Status",
                table: "Orders",
                sql: "\"Status\" IN ('Paid', 'Shipped', 'Delivered', 'Cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderDetails_Quantity_Positive",
                table: "OrderDetails",
                sql: "\"Quantity\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderDetails_UnitPrice_NonNegative",
                table: "OrderDetails",
                sql: "\"UnitPrice\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CartItems_Quantity_Positive",
                table: "CartItems",
                sql: "\"Quantity\" > 0");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Products_ProductId",
                table: "OrderDetails",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Clients_ClientId",
                table: "Orders",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ProductCategories_ProductCategoryId",
                table: "Products",
                column: "ProductCategoryId",
                principalTable: "ProductCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_AspNetUsers_UserId",
                table: "RefreshTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Products_ProductId",
                table: "OrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Clients_ClientId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_ProductCategories_ProductCategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_AspNetUsers_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProductCategoryId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ClientId_PlacedAt",
                table: "Orders");

            migrationBuilder.Sql("""DROP INDEX "UX_ProductCategories_Name_Lower";""");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_Dimensions_Positive",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_Price_Positive",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_Stock_NonNegative",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Amounts",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Status",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderDetails_Quantity_Positive",
                table: "OrderDetails");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderDetails_UnitPrice_NonNegative",
                table: "OrderDetails");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CartItems_Quantity_Positive",
                table: "CartItems");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Clients",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDate",
                table: "Clients",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql(
                """
                UPDATE "Clients"
                SET "Address" = CASE
                        WHEN "Street" IS NULL THEN 'Pendiente de completar'
                        ELSE concat_ws(', ', "Street", "City", "Province")
                    END,
                    "Phone" = COALESCE("Phone", '+10000000000'),
                    "BirthDate" = now() - interval '18 years 1 day';
                """
            );

            migrationBuilder.DropColumn(
                name: "Street",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "DeliveryNotes",
                table: "Clients");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Clients",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Clients",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryDate",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql(
                """
                UPDATE "Orders"
                SET "DeliveryDate" = COALESCE("DeliveredAt", ("EstimatedDeliveryDate"::timestamp AT TIME ZONE 'UTC')),
                    "Status" = CASE "Status"
                        WHEN 'Paid' THEN '1'
                        WHEN 'Shipped' THEN '2'
                        WHEN 'Delivered' THEN '3'
                        WHEN 'Cancelled' THEN '4'
                    END;
                """
            );

            migrationBuilder.Sql("""ALTER TABLE "Orders" ALTER COLUMN "Status" TYPE integer USING "Status"::integer;""");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingCost",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShipToName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShipToPhone",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShipToStreet",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShipToCity",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShipToProvince",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShipToDeliveryNotes",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "EstimatedDeliveryDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "Orders");

            migrationBuilder.AlterColumn<decimal>(
                name: "Total",
                table: "Orders",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.RenameColumn(
                name: "PlacedAt",
                table: "Orders",
                newName: "OrderDate");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "OrderDetails",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "WidthCm",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DepthCm",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Material",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Products");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.Sql("""DELETE FROM "RefreshTokens";""");

            migrationBuilder.DropColumn(
                name: "TokenHash",
                table: "RefreshTokens");

            migrationBuilder.AddColumn<string>(
                name: "Token",
                table: "RefreshTokens",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Products_ProductId",
                table: "OrderDetails",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
