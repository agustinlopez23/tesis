using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Marketplace.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerActivityAndRankingPolicyLabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "owner_activity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_owner_activity", x => x.id);
                    table.CheckConstraint("CK_owner_activity_kind", "kind IN ('profile_update', 'catalog_update', 'promotion_update')");
                    table.ForeignKey(
                        name: "FK_owner_activity_businesses_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_owner_activity_business_id_kind_occurred_at",
                table: "owner_activity",
                columns: new[] { "business_id", "kind", "occurred_at" });

            // Ranking tables are SQL-managed; the label names the published policy (e.g. equity-v1).
            migrationBuilder.Sql("""
                ALTER TABLE ranking_snapshots ADD COLUMN policy_label character varying(40) NULL
                    CONSTRAINT "CK_ranking_snapshots_policy_label" CHECK (policy_label ~ '^[a-z0-9][a-z0-9.-]{0,39}$');
                ALTER TABLE ranking_opportunities ADD COLUMN policy_label character varying(40) NULL
                    CONSTRAINT "CK_ranking_opportunities_policy_label" CHECK (policy_label ~ '^[a-z0-9][a-z0-9.-]{0,39}$');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE ranking_snapshots DROP COLUMN policy_label;
                ALTER TABLE ranking_opportunities DROP COLUMN policy_label;
                """);
            migrationBuilder.DropTable(
                name: "owner_activity");
        }
    }
}
