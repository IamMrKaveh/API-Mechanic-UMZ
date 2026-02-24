namespace Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateTimestamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateTimestamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateTimestamps(DbContext? context)
    {
        if (context is null) return;

        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                var createdAtProp = entry.Properties
                    .FirstOrDefault(p => p.Metadata.Name is "CreatedAt" or "Timestamp" or "OccurredOn" or "UsedAt" or "LastAttempt");

                if (createdAtProp is not null &&
                    (createdAtProp.CurrentValue is null ||
                     (createdAtProp.CurrentValue is DateTime dt && dt == default)))
                    createdAtProp.CurrentValue = now;
            }

            if (entry.State == EntityState.Modified)
            {
                var updatedAtProp = entry.Properties
                    .FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");

                if (updatedAtProp is not null)
                    updatedAtProp.CurrentValue = now;
            }
        }
    }
}