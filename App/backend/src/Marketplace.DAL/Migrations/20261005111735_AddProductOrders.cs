using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddProductOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    business_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    whatsapp = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    access_key_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    access_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    contact_received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_orders", x => x.id);
                    table.CheckConstraint("CK_order_hashes", "access_key_hash ~ '^[0-9a-f]{64}$' AND request_hash ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("CK_order_source", "source = 'catalog'");
                    table.CheckConstraint("CK_order_status", "status IN ('pending','in_attention','cancelled')");
                    table.CheckConstraint("CK_order_version", "version > 0");
                    table.ForeignKey(
                        name: "FK_customer_orders_businesses_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_changes",
                columns: table => new
                {
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    from_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    to_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    contact_received = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_changes", x => new { x.order_id, x.version });
                    table.ForeignKey(
                        name: "FK_order_changes_customer_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "customer_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_changes_user_accounts_actor_id",
                        column: x => x.actor_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_lines",
                columns: table => new
                {
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    product_version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_lines", x => new { x.order_id, x.product_id });
                    table.CheckConstraint("CK_order_line_currency", "currency IN ('ARS','USD')");
                    table.CheckConstraint("CK_order_line_price", "unit_price >= 0");
                    table.CheckConstraint("CK_order_line_quantity", "quantity BETWEEN 1 AND 99");
                    table.ForeignKey(
                        name: "FK_order_lines_catalog_products_product_id",
                        column: x => x.product_id,
                        principalTable: "catalog_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_lines_customer_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "customer_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_orders_business_id_created_at_id",
                table: "customer_orders",
                columns: new[] { "business_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_orders_reference",
                table: "customer_orders",
                column: "reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_changes_actor_id",
                table: "order_changes",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_lines_product_id",
                table: "order_lines",
                column: "product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM customer_orders) THEN
                        RAISE EXCEPTION 'Cannot downgrade persisted customer orders; retain the additive schema when reverting application code.'
                            USING ERRCODE = '23514';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "order_changes");

            migrationBuilder.DropTable(
                name: "order_lines");

            migrationBuilder.DropTable(
                name: "customer_orders");
        }
    }
}
