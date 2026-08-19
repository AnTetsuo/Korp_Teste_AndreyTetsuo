using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "invoicing");

            migrationBuilder.CreateSequence(
                name: "invoice_number_seq",
                schema: "invoicing");

            migrationBuilder.CreateTable(
                name: "invoices",
                schema: "invoicing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invoice_items",
                schema: "invoicing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoice_items", x => x.id);
                    table.CheckConstraint("ck_invoice_items_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_invoice_items_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "invoicing",
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_invoice_items_invoice_id_product_id",
                schema: "invoicing",
                table: "invoice_items",
                columns: new[] { "invoice_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_number",
                schema: "invoicing",
                table: "invoices",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_status",
                schema: "invoicing",
                table: "invoices",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invoice_items",
                schema: "invoicing");

            migrationBuilder.DropTable(
                name: "invoices",
                schema: "invoicing");

            migrationBuilder.DropSequence(
                name: "invoice_number_seq",
                schema: "invoicing");
        }
    }
}
