using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ECommerce.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ActiveCartItemUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductBucketItems_ProductBucketId_ProductId",
                table: "ProductBucketItems");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBucketItems_ProductBucketId_ProductId",
                table: "ProductBucketItems",
                columns: new[] { "ProductBucketId", "ProductId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductBucketItems_ProductBucketId_ProductId",
                table: "ProductBucketItems");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBucketItems_ProductBucketId_ProductId",
                table: "ProductBucketItems",
                columns: new[] { "ProductBucketId", "ProductId" },
                unique: true);
        }
    }
}
