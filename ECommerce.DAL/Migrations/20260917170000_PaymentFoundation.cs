using Microsoft.EntityFrameworkCore.Migrations;
using ECommerce.DAL.DbContexts;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace ECommerce.DAL.Migrations;

[DbContext(typeof(ECommerceDbContext))]
[Migration("20260917170000_PaymentFoundation")]
public partial class PaymentFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<DateTime>(
            name: "PaidAt",
            table: "Payments",
            type: "timestamp with time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.Sql("""
            INSERT INTO "PaymentStatuses" ("Id", "Status")
            VALUES
                ('c1b2c3d4-0000-0000-0000-000000000001', 'Pending'),
                ('c1b2c3d4-0000-0000-0000-000000000002', 'Completed'),
                ('c1b2c3d4-0000-0000-0000-000000000003', 'Failed')
            ON CONFLICT ("Id") DO UPDATE
                SET "Status" = EXCLUDED."Status";
            """);

        migrationBuilder.Sql("""
            INSERT INTO "PaymentMethods" ("Id", "Method")
            VALUES
                ('d1b2c3d4-0000-0000-0000-000000000001', 'Card'),
                ('d1b2c3d4-0000-0000-0000-000000000002', 'Cash'),
                ('d1b2c3d4-0000-0000-0000-000000000003', 'BankTransfer')
            ON CONFLICT ("Id") DO UPDATE
                SET "Method" = EXCLUDED."Method";
            """);

        migrationBuilder.AddColumn<string>(
            name: "CompletionIdempotencyKey",
            table: "Payments",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "IdempotencyKey",
            table: "Payments",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.DropIndex(
            name: "IX_Payments_OrderId",
            table: "Payments");

        migrationBuilder.CreateIndex(
            name: "IX_Payments_OrderId",
            table: "Payments",
            column: "OrderId",
            unique: true,
            filter: "\"PaymentStatusId\" IN ('c1b2c3d4-0000-0000-0000-000000000001', 'c1b2c3d4-0000-0000-0000-000000000002')");

        migrationBuilder.CreateIndex(
            name: "IX_Payments_IdempotencyKey",
            table: "Payments",
            column: "IdempotencyKey",
            unique: true,
            filter: "\"IdempotencyKey\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Payments_CompletionIdempotencyKey",
            table: "Payments",
            column: "CompletionIdempotencyKey",
            unique: true,
            filter: "\"CompletionIdempotencyKey\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Payments_TransactionId",
            table: "Payments",
            column: "TransactionId",
            unique: true,
            filter: "\"TransactionId\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Payments_OrderId",
            table: "Payments");

        migrationBuilder.DropIndex(
            name: "IX_Payments_IdempotencyKey",
            table: "Payments");

        migrationBuilder.DropIndex(
            name: "IX_Payments_CompletionIdempotencyKey",
            table: "Payments");

        migrationBuilder.DropIndex(
            name: "IX_Payments_TransactionId",
            table: "Payments");

        migrationBuilder.Sql("""
            DELETE FROM "PaymentStatuses"
            WHERE "Id" IN (
                'c1b2c3d4-0000-0000-0000-000000000001',
                'c1b2c3d4-0000-0000-0000-000000000002',
                'c1b2c3d4-0000-0000-0000-000000000003');
            """);

        migrationBuilder.Sql("""
            DELETE FROM "PaymentMethods"
            WHERE "Id" IN (
                'd1b2c3d4-0000-0000-0000-000000000001',
                'd1b2c3d4-0000-0000-0000-000000000002',
                'd1b2c3d4-0000-0000-0000-000000000003');
            """);

        migrationBuilder.CreateIndex(
            name: "IX_Payments_OrderId",
            table: "Payments",
            column: "OrderId");

        migrationBuilder.DropColumn(
            name: "CompletionIdempotencyKey",
            table: "Payments");

        migrationBuilder.DropColumn(
            name: "IdempotencyKey",
            table: "Payments");

        migrationBuilder.Sql(
            "UPDATE \"Payments\" SET \"PaidAt\" = \"CreatedAt\" WHERE \"PaidAt\" IS NULL;");

        migrationBuilder.AlterColumn<DateTime>(
            name: "PaidAt",
            table: "Payments",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldNullable: true);
    }
}
