using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Furnistore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenUsedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UsedAt",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsedAt",
                table: "RefreshTokens");
        }
    }
}
