using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AllowRankingActivityVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_activity_context",
                table: "business_activity");

            migrationBuilder.AlterColumn<string>(
                name: "ranking_version",
                table: "business_activity",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_activity_context",
                table: "business_activity",
                sql: "(source IN ('directory', 'search') AND position IS NOT NULL AND ranking_version IS NOT NULL AND position BETWEEN 1 AND 200000 AND (ranking_version IN ('name-v1', 'distance-v1') OR (source = 'directory' AND ranking_version ~ '^[0-9a-f]{64}$'))) OR (source = 'unknown' AND position IS NULL AND ranking_version IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Never erase attribution or truncate policy hashes to make an old schema fit.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM business_activity WHERE ranking_version ~ '^[0-9a-f]{64}$') THEN
                        RAISE EXCEPTION 'Ranking activity must expire and be purged before downgrading.' USING ERRCODE = '23514';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropCheckConstraint(
                name: "CK_activity_context",
                table: "business_activity");

            migrationBuilder.AlterColumn<string>(
                name: "ranking_version",
                table: "business_activity",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_activity_context",
                table: "business_activity",
                sql: "(source IN ('directory', 'search') AND position BETWEEN 1 AND 200000 AND ranking_version IN ('name-v1', 'distance-v1')) OR (source = 'unknown' AND position IS NULL AND ranking_version IS NULL)");
        }
    }
}
