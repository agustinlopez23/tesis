using System.Data;
using System.Text.Json;
using Marketplace.BLL;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL;

public sealed class RankingSnapshotStore(MarketplaceDbContext database, TimeProvider clock, RankingReadLimits limits) : IRankingSnapshotStore
{
    public async Task<SnapshotResult> CreateAsync(Guid requestId, string contextKey,
        Func<long, CancellationToken, Task<RankingOpportunity>> build, CancellationToken ct)
    {
        if (requestId == Guid.Empty || string.IsNullOrWhiteSpace(contextKey) || contextKey.Length > 200)
            throw new ArgumentException("Invalid snapshot request.");
        ArgumentNullException.ThrowIfNull(build);
        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        // Always request lock before context lock; collisions only serialize unrelated requests.
        var lockKey = "ranking-request:" + requestId.ToString("D");
        await database.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct);
        var existing = await database.Database.SqlQuery<StoredSnapshot>($"""
            SELECT context_key AS "ContextKey", expires_at AS "ExpiresAt", payload::text AS "Payload"
            FROM ranking_snapshots WHERE request_id = {requestId}
            """).SingleOrDefaultAsync(ct);
        if (existing is not null)
        {
            if (existing.ContextKey != contextKey) return new(SnapshotStatus.Conflict);
            return existing.ExpiresAt <= clock.GetUtcNow() ? new(SnapshotStatus.Expired)
                : new(SnapshotStatus.Existing, Deserialize(existing.Payload));
        }
        // The row lock lasts until commit, so the next builder sees this one's contributions.
        var sequences = await database.Database.SqlQuery<long>($"""
            INSERT INTO ranking_contexts (context_key, sequence) VALUES ({contextKey}, 1)
            ON CONFLICT (context_key) DO UPDATE SET sequence = ranking_contexts.sequence + 1
            RETURNING sequence AS "Value"
            """).ToListAsync(ct);
        var sequence = sequences.Single();
        var opportunity = await build(sequence, ct);
        ct.ThrowIfCancellationRequested();
        if (opportunity.Context.Key != contextKey || opportunity.Context.Sequence != sequence ||
            opportunity.Id == Guid.Empty || opportunity.Context.At > clock.GetUtcNow())
            throw new ArgumentException("Opportunity does not match its transaction.");
        var snapshot = new RankingSnapshot(opportunity.Id, contextKey, opportunity.Context.At,
            opportunity.Context.At.AddMinutes(30), opportunity.PolicyHash,
            Array.AsReadOnly(opportunity.Candidates.Select(x => x.Candidate.Id).ToArray()), opportunity.PolicyLabel);
        var snapshotJson = JsonSerializer.Serialize(snapshot);
        var opportunityJson = JsonSerializer.Serialize(opportunity);
        var traceJson = JsonSerializer.Serialize(opportunity.Candidates);
        await database.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO ranking_snapshots (id, request_id, context_key, expires_at, payload, policy_label)
            VALUES ({snapshot.Id}, {requestId}, {contextKey}, {snapshot.ExpiresAt}, CAST({snapshotJson} AS jsonb), {opportunity.PolicyLabel});
            INSERT INTO ranking_opportunities (id, context_key, sequence, occurred_at, payload, policy_label)
            VALUES ({opportunity.Id}, {contextKey}, {sequence}, {opportunity.Context.At}, CAST({opportunityJson} AS jsonb), {opportunity.PolicyLabel});
            INSERT INTO ranking_contributions (opportunity_id, business_id, expected, received)
            SELECT {opportunity.Id}, (item->'Candidate'->>'Id')::uuid,
                (item->>'Expected')::double precision, (item->>'Received')::double precision
            FROM jsonb_array_elements(CAST({traceJson} AS jsonb)) item;
            """, ct);
        await transaction.CommitAsync(ct);
        return new(SnapshotStatus.Created, snapshot);
    }

    public async Task<IReadOnlyDictionary<Guid, ExposureBalance>> ReadBalancesAsync(string contextKey, DateTimeOffset at, CancellationToken ct)
    {
        var rows = await database.Database.SqlQuery<BalanceRow>($"""
            SELECT c.business_id AS "Id", sum(c.expected) AS "Expected", sum(c.received) AS "Received"
            FROM ranking_contributions c JOIN ranking_opportunities o ON o.id = c.opportunity_id
            WHERE o.context_key = {contextKey} AND o.occurred_at > {at.AddDays(-30)} AND o.occurred_at <= {at}
            GROUP BY c.business_id
            """).Take(limits.MaxBalanceRows + 1).ToListAsync(ct);
        if (rows.Count > limits.MaxBalanceRows) throw new RankingCapacityException();
        return rows.ToDictionary(x => x.Id, x => new ExposureBalance(x.Expected, x.Received));
    }

    public async Task<RankingSnapshot?> FindAsync(Guid id, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var row = await database.Database.SqlQuery<string>($"""
            SELECT payload::text AS "Value" FROM ranking_snapshots WHERE id = {id} AND expires_at > {now}
            """).SingleOrDefaultAsync(ct);
        return row is null ? null : Deserialize(row);
    }

    public async Task PurgeAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        await database.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM ranking_snapshots WHERE expires_at <= {now};
            DELETE FROM ranking_opportunities WHERE occurred_at <= {now.AddDays(-30)};
            """, ct);
    }

    private static RankingSnapshot Deserialize(string json)
    {
        var snapshot = JsonSerializer.Deserialize<RankingSnapshot>(json)
            ?? throw new InvalidOperationException("Invalid stored ranking snapshot.");
        return snapshot with { BusinessIds = Array.AsReadOnly(snapshot.BusinessIds.ToArray()) };
    }
    private sealed record StoredSnapshot(string ContextKey, DateTimeOffset ExpiresAt, string Payload);
    private sealed record BalanceRow(Guid Id, double Expected, double Received);
}
