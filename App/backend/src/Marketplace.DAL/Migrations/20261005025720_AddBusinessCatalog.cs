using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "business_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_business_subscriptions", x => x.id);
                    table.CheckConstraint("CK_subscription_dates", "ends_at > starts_at");
                    table.CheckConstraint("CK_subscription_revocation", "(revoked_at IS NULL AND revoked_by IS NULL AND revocation_reason IS NULL) OR (revoked_at IS NOT NULL AND revoked_by IS NOT NULL AND revocation_reason IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_business_subscriptions_businesses_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_business_subscriptions_user_accounts_granted_by",
                        column: x => x.granted_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_business_subscriptions_user_accounts_revoked_by",
                        column: x => x.revoked_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "catalog_products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    available = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_products", x => x.id);
                    table.CheckConstraint("CK_product_currency", "currency IN ('ARS','USD')");
                    table.CheckConstraint("CK_product_price", "price >= 0");
                    table.CheckConstraint("CK_product_status", "status IN ('draft','published','archived')");
                    table.CheckConstraint("CK_product_version", "version > 0");
                    table.ForeignKey(
                        name: "FK_catalog_products_businesses_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_photos",
                columns: table => new
                {
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_photos", x => x.product_id);
                    table.CheckConstraint("CK_product_photo_size", "octet_length(content) BETWEEN 1 AND 1048576");
                    table.ForeignKey(
                        name: "FK_product_photos_catalog_products_product_id",
                        column: x => x.product_id,
                        principalTable: "catalog_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "name" },
                values: new object[] { (short)2, "administrator" });

            migrationBuilder.CreateIndex(
                name: "IX_business_subscriptions_business_id_starts_at_ends_at",
                table: "business_subscriptions",
                columns: new[] { "business_id", "starts_at", "ends_at" });

            migrationBuilder.CreateIndex(
                name: "IX_business_subscriptions_granted_by",
                table: "business_subscriptions",
                column: "granted_by");

            migrationBuilder.CreateIndex(
                name: "IX_business_subscriptions_revoked_by",
                table: "business_subscriptions",
                column: "revoked_by");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_products_business_id_status_name_id",
                table: "catalog_products",
                columns: new[] { "business_id", "status", "name", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM catalog_products)
                       OR EXISTS (SELECT 1 FROM business_subscriptions)
                       OR EXISTS (SELECT 1 FROM user_account_roles WHERE role_id = 2) THEN
                        RAISE EXCEPTION 'Cannot downgrade a populated catalog or administrator assignments; retain schema when reverting application code.'
                            USING ERRCODE = '23514';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "business_subscriptions");

            migrationBuilder.DropTable(
                name: "product_photos");

            migrationBuilder.DropTable(
                name: "catalog_products");

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: (short)2);
        }
    }
}
