using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_order_status",
                table: "customer_orders");

            migrationBuilder.CreateTable(
                name: "inventory_balances",
                columns: table => new
                {
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_balances", x => x.product_id);
                    table.CheckConstraint("CK_inventory_quantity", "quantity BETWEEN 0 AND 1000000");
                    table.CheckConstraint("CK_inventory_version", "version > 0");
                    table.ForeignKey(
                        name: "FK_inventory_balances_catalog_products_product_id",
                        column: x => x.product_id,
                        principalTable: "catalog_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    void_operation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    void_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    void_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    voided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    voided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales", x => x.id);
                    table.ForeignKey(
                        name: "FK_sales_businesses_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_customer_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "customer_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_user_accounts_actor_id",
                        column: x => x.actor_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_user_accounts_voided_by",
                        column: x => x.voided_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_movements",
                columns: table => new
                {
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: true),
                    delta = table.Column<int>(type: "integer", nullable: false),
                    balance = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_movements", x => new { x.operation_id, x.product_id });
                    table.CheckConstraint("CK_movement_balance", "balance BETWEEN 0 AND 1000000");
                    table.CheckConstraint("CK_movement_kind", "kind IN ('adjustment','sale','return')");
                    table.ForeignKey(
                        name: "FK_inventory_movements_businesses_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_movements_catalog_products_product_id",
                        column: x => x.product_id,
                        principalTable: "catalog_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_movements_sales_sale_id",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_movements_user_accounts_actor_id",
                        column: x => x.actor_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sale_lines",
                columns: table => new
                {
                    sale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    origin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    returned_quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_lines", x => new { x.sale_id, x.product_id });
                    table.CheckConstraint("CK_sale_origin", "origin IN ('requested','conversation','direct')");
                    table.CheckConstraint("CK_sale_price", "unit_price >= 0 AND currency IN ('ARS','USD')");
                    table.CheckConstraint("CK_sale_quantity", "quantity BETWEEN 1 AND 99 AND returned_quantity BETWEEN 0 AND quantity");
                    table.ForeignKey(
                        name: "FK_sale_lines_catalog_products_product_id",
                        column: x => x.product_id,
                        principalTable: "catalog_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sale_lines_sales_sale_id",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_order_status",
                table: "customer_orders",
                sql: "status IN ('pending','in_attention','cancelled','converted')");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_actor_id",
                table: "inventory_movements",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_business_id_created_at_operation_id",
                table: "inventory_movements",
                columns: new[] { "business_id", "created_at", "operation_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_product_id",
                table: "inventory_movements",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_sale_id",
                table: "inventory_movements",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_lines_product_id",
                table: "sale_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_actor_id",
                table: "sales",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_business_id_created_at_id",
                table: "sales",
                columns: new[] { "business_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_id",
                table: "sales",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_voided_by",
                table: "sales",
                column: "voided_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM inventory_balances) OR EXISTS (SELECT 1 FROM inventory_movements)
                       OR EXISTS (SELECT 1 FROM sales) OR EXISTS (SELECT 1 FROM customer_orders WHERE status='converted') THEN
                        RAISE EXCEPTION 'Cannot downgrade populated sales or inventory history' USING ERRCODE = '23514';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "inventory_balances");

            migrationBuilder.DropTable(
                name: "inventory_movements");

            migrationBuilder.DropTable(
                name: "sale_lines");

            migrationBuilder.DropTable(
                name: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "CK_order_status",
                table: "customer_orders");

            migrationBuilder.AddCheckConstraint(
                name: "CK_order_status",
                table: "customer_orders",
                sql: "status IN ('pending','in_attention','cancelled')");
        }
    }
}
