using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Furnistore.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertPricesToCup : Migration
    {
        private const int CupPerUnit = 800;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                UPDATE "Products"
                SET "Price" = GREATEST(ROUND("Price" * {CupPerUnit} / 100) * 100, 100);

                UPDATE "Orders"
                SET "Subtotal" = "Subtotal" * {CupPerUnit},
                    "ShippingCost" = "ShippingCost" * {CupPerUnit},
                    "Total" = "Total" * {CupPerUnit};

                UPDATE "OrderDetails"
                SET "UnitPrice" = "UnitPrice" * {CupPerUnit};
                """);

            migrationBuilder.Sql(ScaleAuditAmounts("price", $"* {CupPerUnit}"));
            migrationBuilder.Sql(ScaleAuditAmounts("total", $"* {CupPerUnit}"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                UPDATE "Products"
                SET "Price" = ROUND("Price" / {CupPerUnit}, 2);

                UPDATE "Orders"
                SET "Subtotal" = ROUND("Subtotal" / {CupPerUnit}, 2),
                    "ShippingCost" = ROUND("ShippingCost" / {CupPerUnit}, 2),
                    "Total" = ROUND("Total" / {CupPerUnit}, 2);

                UPDATE "OrderDetails"
                SET "UnitPrice" = ROUND("UnitPrice" / {CupPerUnit}, 2);
                """);

            migrationBuilder.Sql(ScaleAuditAmounts("price", $"/ {CupPerUnit}"));
            migrationBuilder.Sql(ScaleAuditAmounts("total", $"/ {CupPerUnit}"));
        }

        private static string ScaleAuditAmounts(string field, string operation) => $$"""
            UPDATE "AuditEntries"
            SET "Changes" = jsonb_set(
                "Changes",
                '{{{field}}}',
                jsonb_build_object(
                    'from', to_jsonb(trim_scale(ROUND(("Changes" -> '{{field}}' ->> 'from')::numeric {{operation}}, 2))::text),
                    'to', to_jsonb(trim_scale(ROUND(("Changes" -> '{{field}}' ->> 'to')::numeric {{operation}}, 2))::text)
                )
            )
            WHERE "Changes" -> '{{field}}' IS NOT NULL;
            """;
    }
}
