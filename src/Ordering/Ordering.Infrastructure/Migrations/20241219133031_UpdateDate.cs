using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedOn",
                table: "OrderItems",
                newName: "UpdatedOnUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedOn",
                table: "OrderItems",
                newName: "CreatedOnUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedOnUtc",
                table: "OrderItems",
                newName: "UpdatedOn");

            migrationBuilder.RenameColumn(
                name: "CreatedOnUtc",
                table: "OrderItems",
                newName: "CreatedOn");
        }
    }
}
