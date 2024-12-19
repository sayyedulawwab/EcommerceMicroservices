using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedOn",
                table: "Products",
                newName: "UpdatedOnUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedOn",
                table: "Products",
                newName: "CreatedOnUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedOn",
                table: "Categories",
                newName: "UpdatedOnUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedOn",
                table: "Categories",
                newName: "CreatedOnUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedOnUtc",
                table: "Products",
                newName: "UpdatedOn");

            migrationBuilder.RenameColumn(
                name: "CreatedOnUtc",
                table: "Products",
                newName: "CreatedOn");

            migrationBuilder.RenameColumn(
                name: "UpdatedOnUtc",
                table: "Categories",
                newName: "UpdatedOn");

            migrationBuilder.RenameColumn(
                name: "CreatedOnUtc",
                table: "Categories",
                newName: "CreatedOn");
        }
    }
}
