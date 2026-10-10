using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Furnistore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClientIdentityCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdentityCard",
                table: "Clients",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_IdentityCard",
                table: "Clients",
                column: "IdentityCard",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clients_IdentityCard",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "IdentityCard",
                table: "Clients");
        }
    }
}
