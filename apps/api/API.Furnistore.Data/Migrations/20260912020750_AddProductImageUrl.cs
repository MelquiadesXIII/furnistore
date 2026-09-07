using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Furnistore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Products" AS p
                SET "ImageUrl" = v.url
                FROM (VALUES
                    (1, 'https://yjckqaxvulvdlvaenqzk.supabase.co/storage/v1/object/public/products/01-silla-roble-nordico.jpg'),
                    (2, 'https://yjckqaxvulvdlvaenqzk.supabase.co/storage/v1/object/public/products/02-silla-tapizada-lino.jpg'),
                    (3, 'https://yjckqaxvulvdlvaenqzk.supabase.co/storage/v1/object/public/products/03-silla-alta-bar-teca.jpg'),
                    (4, 'https://yjckqaxvulvdlvaenqzk.supabase.co/storage/v1/object/public/products/04-silla-plegable-bambu.jpg'),
                    (5, 'https://yjckqaxvulvdlvaenqzk.supabase.co/storage/v1/object/public/products/05-mesa-centro-nogal.jpg'),
                    (6, 'https://yjckqaxvulvdlvaenqzk.supabase.co/storage/v1/object/public/products/06-mesa-comedor-encino.jpg'),
                    (7, 'https://yjckqaxvulvdlvaenqzk.supabase.co/storage/v1/object/public/products/07-mesa-auxiliar-marmol.jpg'),
                    (8, 'https://yjckqaxvulvdlvaenqzk.supabase.co/storage/v1/object/public/products/08-mesa-escritorio-minimalista.jpg'),
                    (9, 'https://yjckqaxvulvdlvaenqzk.supabase.co/storage/v1/object/public/products/09-estanteria-modular-roble.jpg'),
                    (10, 'https://yjckqaxvulvdlvaenqzk.supabase.co/storage/v1/object/public/products/10-librero-escalera-pino.jpg'),
                    (12, 'https://yjckqaxvulvdlvaenqzk.supabase.co/storage/v1/object/public/products/12-vitrina-vintage.jpg'),
                    (13, 'https://yjckqaxvulvdlvaenqzk.supabase.co/storage/v1/object/public/products/13-lampara-pie-arco.jpg'),
                    (14, 'https://yjckqaxvulvdlvaenqzk.supabase.co/storage/v1/object/public/products/14-lampara-mesa-ceramica.jpg'),
                    (15, 'https://yjckqaxvulvdlvaenqzk.supabase.co/storage/v1/object/public/products/15-lampara-colgante-rattan.jpg'),
                    (16, 'https://yjckqaxvulvdlvaenqzk.supabase.co/storage/v1/object/public/products/16-lampara-led-escritorio.jpg')
                ) AS v(id, url)
                WHERE p."Id" = v.id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Products");
        }
    }
}
