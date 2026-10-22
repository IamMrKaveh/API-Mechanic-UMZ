using System.Diagnostics;
using Application.Common.Contracts;
using Domain.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SharedContracts.Diagnostics;

namespace Infrastructure.Persistence.Outbox;

public sealed class OutboxProcessor(
    DBContext context,
    IDomainEventDispatcher dispatcher,
    IOutboxEventTypeRegistry typeRegistry,
    IAuditService auditService) : IOutboxProcessor
{
    private const int MaxRetries = 5;

    public async Task ProcessAsync(
        int batchSize = 50,
        CancellationToken ct = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await context.Database.BeginTransactionAsync(ct);

            var messages = await context.OutboxMessages
                .FromSqlInterpolated($"""
                    SELECT * FROM "OutboxMessages"
                    WHERE "processed_at" IS NULL
                      AND "is_poisoned" = false
                      AND "retry_count" < {MaxRetries}
                    ORDER BY "created_at", "sequence", "id"
                    LIMIT {batchSize}
                    FOR UPDATE SKIP LOCKED
                    """)
                .ToListAsync(ct);

            if (messages.Count == 0)
            {
                await transaction.RollbackAsync(ct);
                return;
            }

            foreach (var message in messages)
            {
                await ProcessSingleAsync(message, ct);
            }

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        });
    }

    private async Task ProcessSingleAsync(OutboxMessage message, CancellationToken ct)
    {
        using var activity = ApplicationActivitySources.Outbox.StartActivity(
            "outbox.process",
            ActivityKind.Consumer,
            parentId: message.TraceParent ?? string.Empty);

        if (activity is not null && !string.IsNullOrWhiteSpace(message.TraceState))
            activity.TraceStateString = message.TraceState;

        activity?.SetTag("outbox.message_id", message.Id.Value);
        activity?.SetTag("outbox.event_type", message.Type);
        activity?.SetTag("outbox.retry_count", message.RetryCount);
        activity?.SetTag("outbox.sequence", message.Sequence);

        // The state of the tracked entities is remembered, so a handler that fails
        // halfway through leaves nothing behind: its writes are rolled back to the
        // savepoint and the change tracker is restored. Otherwise the retry would
        // apply them a second time.
        var snapshot = TrackedEntities.TakeSnapshot(context.ChangeTracker);

        if (!context.Database.SupportsSavepoints())
        {
            // Without savepoints the failed message keeps its writes, so it is
            // retried with the changes already applied.
            await ProcessWithoutSavepointAsync(message, ct);
            return;
        }

        var savepoint = $"sp_outbox_{Guid.NewGuid():N}";

        try
        {
            await context.Database.CreateSavepointAsync(savepoint, ct);

            var type = typeRegistry.Resolve(message.Type);

            if (type is null)
            {
                await auditService.LogWarningAsync(
                    $"Could not resolve type {message.Type} for outbox message {message.Id.Value}",
                    ct);

                message.MarkPoisoned($"Unresolvable event type: {message.Type}");
                activity?.SetStatus(ActivityStatusCode.Error, "Unresolvable event type.");
                return;
            }

            var @event = JsonSerializer.Deserialize(message.Payload, type, DomainEventJson.Options);

            if (@event is null)
            {
                message.MarkPoisoned("Deserialization returned null payload.");
                activity?.SetStatus(ActivityStatusCode.Error, "Null payload after deserialization.");
                return;
            }

            await dispatcher.DispatchAsync([(IDomainEvent)@event], ct);

            message.MarkProcessed(DateTime.UtcNow);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"Error processing outbox message {message.Id.Value}: {ex.Message}",
                ct);

            await context.Database.RollbackToSavepointAsync(savepoint, ct);
            snapshot.Restore(context.ChangeTracker);

            message.MarkFailed(ex.Message);

            if (message.RetryCount >= MaxRetries)
                message.MarkPoisoned($"Exceeded max retries ({MaxRetries}): {ex.Message}");

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
        }
    }

    /// <summary>
    /// Fallback for a transaction that cannot take savepoints: the failed message
    /// is reported, but its writes stay in the transaction.
    /// </summary>
    private async Task ProcessWithoutSavepointAsync(OutboxMessage message, CancellationToken ct)
    {
        try
        {
            var type = typeRegistry.Resolve(message.Type);

            if (type is null)
            {
                message.MarkPoisoned($"Unresolvable event type: {message.Type}");
                return;
            }

            var @event = JsonSerializer.Deserialize(message.Payload, type, DomainEventJson.Options);

            if (@event is null)
            {
                message.MarkPoisoned("Deserialization returned null payload.");
                return;
            }

            await dispatcher.DispatchAsync([(IDomainEvent)@event], ct);

            message.MarkProcessed(DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"Error processing outbox message {message.Id.Value}: {ex.Message}",
                ct);

            message.MarkFailed(ex.Message);

            if (message.RetryCount >= MaxRetries)
                message.MarkPoisoned($"Exceeded max retries ({MaxRetries}): {ex.Message}");
        }
    }
}
