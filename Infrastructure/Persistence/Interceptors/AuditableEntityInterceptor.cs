namespace Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntityInterceptor(IDateTimeProvider dateTimeProvider) : SaveChangesInterceptor
{
    private const string RowVersionPropertyName = "RowVersion";

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context is null) return;

        var now = dateTimeProvider.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(IAuditable.CreatedAt)).CurrentValue = now;
                entry.Property(nameof(IAuditable.UpdatedAt)).CurrentValue = now;
                continue;
            }

            if (entry.State == EntityState.Modified)
            {
                var createdAtProperty = entry.Property(nameof(IAuditable.CreatedAt));
                createdAtProperty.IsModified = false;
                createdAtProperty.CurrentValue = createdAtProperty.OriginalValue;

                entry.Property(nameof(IAuditable.UpdatedAt)).CurrentValue = now;
            }
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            var rowVersionProperty = entry.Metadata.FindProperty(RowVersionPropertyName);
            if (rowVersionProperty is null || rowVersionProperty.ClrType != typeof(byte[]))
                continue;

            entry.Property(RowVersionPropertyName).CurrentValue = Guid.NewGuid().ToByteArray();
        }
    }
}
