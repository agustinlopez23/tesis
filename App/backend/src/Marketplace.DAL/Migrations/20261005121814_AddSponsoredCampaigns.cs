using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddSponsoredCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_order_source",
                table: "customer_orders");

            migrationBuilder.AddColumn<Guid>(
                name: "campaign_id",
                table: "customer_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    photo_slot = table.Column<int>(type: "integer", nullable: true),
                    image = table.Column<byte[]>(type: "bytea", nullable: true),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    conditions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    zone_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    save_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    payment_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaigns", x => x.id);
                    table.CheckConstraint("CK_campaign_dates", "ends_at > starts_at AND ends_at <= starts_at + interval '30 days'");
                    table.CheckConstraint("CK_campaign_destination", "(product_id IS NOT NULL AND photo_slot IS NULL) OR (product_id IS NULL AND photo_slot BETWEEN 1 AND 5)");
                    table.CheckConstraint("CK_campaign_image", "image IS NULL OR octet_length(image) <= 1048576");
                    table.CheckConstraint("CK_campaign_status", "status IN ('draft','pending_review','awaiting_payment','scheduled','paused','rejected')");
                    table.CheckConstraint("CK_campaign_zone", "zone_id IN ('centro','nueva-cordoba','alberdi')");
                    table.ForeignKey(
                        name: "FK_campaigns_businesses_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_campaigns_catalog_products_product_id",
                        column: x => x.product_id,
                        principalTable: "catalog_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "campaign_changes",
                columns: table => new
                {
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaign_changes", x => x.operation_id);
                    table.ForeignKey(
                        name: "FK_campaign_changes_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_campaign_changes_user_accounts_actor_id",
                        column: x => x.actor_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "campaign_events",
                columns: table => new
                {
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    context_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaign_events", x => new { x.campaign_id, x.context_id, x.kind });
                    table.CheckConstraint("CK_campaign_event_kind", "kind IN ('impression','click')");
                    table.ForeignKey(
                        name: "FK_campaign_events_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_orders_campaign_id",
                table: "customer_orders",
                column: "campaign_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_order_source",
                table: "customer_orders",
                sql: "(source = 'catalog' AND campaign_id IS NULL) OR (source = 'campaign' AND campaign_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_campaign_changes_actor_id",
                table: "campaign_changes",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_campaign_changes_campaign_id_version",
                table: "campaign_changes",
                columns: new[] { "campaign_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_campaign_events_created_at",
                table: "campaign_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_campaigns_business_id_created_at_id",
                table: "campaigns",
                columns: new[] { "business_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_campaigns_product_id",
                table: "campaigns",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_campaigns_zone_id_starts_at_ends_at",
                table: "campaigns",
                columns: new[] { "zone_id", "starts_at", "ends_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_customer_orders_campaigns_campaign_id",
                table: "customer_orders",
                column: "campaign_id",
                principalTable: "campaigns",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM campaigns) OR EXISTS (SELECT 1 FROM campaign_changes)
                       OR EXISTS (SELECT 1 FROM campaign_events) OR EXISTS (SELECT 1 FROM customer_orders WHERE campaign_id IS NOT NULL) THEN
                        RAISE EXCEPTION 'Cannot downgrade campaign history or attribution' USING ERRCODE = '23514';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_customer_orders_campaigns_campaign_id",
                table: "customer_orders");

            migrationBuilder.DropTable(
                name: "campaign_changes");

            migrationBuilder.DropTable(
                name: "campaign_events");

            migrationBuilder.DropTable(
                name: "campaigns");

            migrationBuilder.DropIndex(
                name: "IX_customer_orders_campaign_id",
                table: "customer_orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_order_source",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "campaign_id",
                table: "customer_orders");

            migrationBuilder.AddCheckConstraint(
                name: "CK_order_source",
                table: "customer_orders",
                sql: "source = 'catalog'");
        }
    }
}
