using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InvoiceSnapshotsAndCheckoutBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Orders_OrderId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_OrderId",
                table: "Invoices");

            // Historical orders predate the store-currency snapshot. USD is a
            // migration-only preservation value; checkout always supplies it.
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Orders",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddColumn<Guid>(
                name: "BillingAddressId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddressSnapshot",
                table: "Orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Payments",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Currency_UppercaseIso4217",
                table: "Orders",
                sql: "\"Currency\" ~ '^[A-Z]{3}$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Currency_UppercaseIso4217",
                table: "Payments",
                sql: "\"Currency\" ~ '^[A-Z]{3}$'");

            // Old CRUD placeholder invoices are retained as historical rows,
            // but deliberately have no manufactured Payment/line snapshots.
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Invoices",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentId",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTransactionId",
                table: "Invoices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "SubtotalAmount",
                table: "Invoices",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Invoices_Currency_UppercaseIso4217",
                table: "Invoices",
                sql: "\"Currency\" ~ '^[A-Z]{3}$'");

            migrationBuilder.CreateTable(
                name: "InvoiceItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OrderId",
                table: "Invoices",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PaymentId",
                table: "Invoices",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_InvoiceId",
                table: "InvoiceItems",
                column: "InvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Orders_OrderId",
                table: "Invoices",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Payments_PaymentId",
                table: "Invoices",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Invoices_Orders_OrderId", table: "Invoices");
            migrationBuilder.DropForeignKey(name: "FK_Invoices_Payments_PaymentId", table: "Invoices");
            migrationBuilder.DropTable(name: "InvoiceItems");
            migrationBuilder.DropIndex(name: "IX_Invoices_OrderId", table: "Invoices");
            migrationBuilder.DropIndex(name: "IX_Invoices_PaymentId", table: "Invoices");

            migrationBuilder.DropCheckConstraint(name: "CK_Orders_Currency_UppercaseIso4217", table: "Orders");
            migrationBuilder.DropCheckConstraint(name: "CK_Payments_Currency_UppercaseIso4217", table: "Payments");
            migrationBuilder.DropCheckConstraint(name: "CK_Invoices_Currency_UppercaseIso4217", table: "Invoices");

            migrationBuilder.DropColumn(name: "Currency", table: "Orders");
            migrationBuilder.DropColumn(name: "BillingAddressId", table: "Orders");
            migrationBuilder.DropColumn(name: "BillingAddressSnapshot", table: "Orders");
            migrationBuilder.DropColumn(name: "Currency", table: "Invoices");
            migrationBuilder.DropColumn(name: "PaymentId", table: "Invoices");
            migrationBuilder.DropColumn(name: "PaymentTransactionId", table: "Invoices");
            migrationBuilder.DropColumn(name: "SubtotalAmount", table: "Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Payments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OrderId",
                table: "Invoices",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Orders_OrderId",
                table: "Invoices",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
