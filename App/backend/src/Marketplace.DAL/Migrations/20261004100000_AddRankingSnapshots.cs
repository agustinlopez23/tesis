using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Marketplace.DAL.Migrations;

// SQL-owned infrastructure tables, deliberately outside the EF business model.
[DbContext(typeof(MarketplaceDbContext))]
[Migration("20261004100000_AddRankingSnapshots")]
public sealed class AddRankingSnapshots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TABLE ranking_contexts (
            context_key varchar(200) PRIMARY KEY,
            sequence bigint NOT NULL CHECK (sequence > 0)
        );
        CREATE TABLE ranking_opportunities (
            id uuid PRIMARY KEY,
            context_key varchar(200) NOT NULL REFERENCES ranking_contexts(context_key),
            sequence bigint NOT NULL CHECK (sequence > 0),
            occurred_at timestamptz NOT NULL,
            payload jsonb NOT NULL CHECK (jsonb_typeof(payload) = 'object'),
            UNIQUE (context_key, sequence)
        );
        CREATE INDEX ranking_opportunities_context_time ON ranking_opportunities(context_key, occurred_at);
        CREATE INDEX ranking_opportunities_time ON ranking_opportunities(occurred_at);
        CREATE TABLE ranking_contributions (
            opportunity_id uuid NOT NULL REFERENCES ranking_opportunities(id) ON DELETE CASCADE,
            business_id uuid NOT NULL,
            expected double precision NOT NULL CHECK (expected >= 0 AND expected <= 1),
            received double precision NOT NULL CHECK (received >= 0 AND received <= 1),
            PRIMARY KEY (opportunity_id, business_id)
        );
        CREATE TABLE ranking_snapshots (
            id uuid PRIMARY KEY,
            request_id uuid NOT NULL UNIQUE,
            context_key varchar(200) NOT NULL REFERENCES ranking_contexts(context_key),
            expires_at timestamptz NOT NULL,
            payload jsonb NOT NULL CHECK (jsonb_typeof(payload) = 'object')
        );
        CREATE INDEX ranking_snapshots_expiry ON ranking_snapshots(expires_at);
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP TABLE ranking_snapshots;
        DROP TABLE ranking_contributions;
        DROP TABLE ranking_opportunities;
        DROP TABLE ranking_contexts;
        """);
}
