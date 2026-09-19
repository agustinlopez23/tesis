using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "business_activity",
                columns: table => new
                {
                    visit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    surface = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: true),
                    ranking_version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_business_activity", x => new { x.visit_id, x.business_id, x.kind, x.surface });
                    table.CheckConstraint("CK_activity_context", "(source IN ('directory', 'search') AND position BETWEEN 1 AND 200000 AND ranking_version IN ('name-v1', 'distance-v1')) OR (source = 'unknown' AND position IS NULL AND ranking_version IS NULL)");
                    table.CheckConstraint("CK_activity_kind", "kind IN ('impression', 'profile_view', 'contact')");
                    table.CheckConstraint("CK_activity_surface", "surface IN ('directory', 'profile') AND (kind <> 'impression' OR surface = 'directory') AND (kind <> 'profile_view' OR surface = 'profile')");
                    table.ForeignKey(
                        name: "FK_business_activity_businesses_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_business_activity_business_id_occurred_at",
                table: "business_activity",
                columns: new[] { "business_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_business_activity_occurred_at",
                table: "business_activity",
                column: "occurred_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "business_activity");
        }
    }
}
