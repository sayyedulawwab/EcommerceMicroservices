using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelledOnUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveredOnUtc",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "ShippedOnUtc",
                table: "Orders",
                newName: "UpdatedOnUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedOnUtc",
                table: "Orders",
                newName: "ShippedOnUtc");

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledOnUtc",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredOnUtc",
                table: "Orders",
                type: "datetime2",
                nullable: true);
        }
    }
}
