using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Persistence.Outbox;

/// <summary>
/// The state of the tracked entities before a message is handled, so a handler
/// that fails can be undone: the entities it added are detached and the ones it
/// changed go back to the values they had. The values of the entities that were
/// already changed by an earlier message are kept, because those changes belong
/// to a message that succeeded.
/// </summary>
internal sealed class TrackedEntities
{
    private readonly List<EntrySnapshot> _entries;

    private TrackedEntities(List<EntrySnapshot> entries) => _entries = entries;

    public static TrackedEntities TakeSnapshot(ChangeTracker changeTracker)
    {
        var entries = new List<EntrySnapshot>(changeTracker.Entries().Count());

        foreach (var entry in changeTracker.Entries())
        {
            var state = entry.State;

            if (state == EntityState.Detached)
            {
                entries.Add(new EntrySnapshot(entry, state, null));
                continue;
            }

            var values = entry.CurrentValues.Properties
                .ToDictionary(p => p.Name, p => entry.CurrentValues[p]);

            entries.Add(new EntrySnapshot(entry, state, values));
        }

        return new TrackedEntities(entries);
    }

    /// <summary>
    /// Puts the tracked entities back into the state they had when the snapshot
    /// was taken.
    /// </summary>
    public void Restore(ChangeTracker changeTracker)
    {
        foreach (var snapshot in _entries)
        {
            var entry = snapshot.Entry;
            var values = snapshot.Values;

            switch (entry.State)
            {
                case EntityState.Added when snapshot.State != EntityState.Added:
                    // The failed message added it, so it must not be saved.
                    entry.State = EntityState.Detached;
                    break;

                case EntityState.Modified:
                case EntityState.Deleted:
                    if (values is null)
                        break;

                    foreach (var (name, value) in values)
                        entry.Property(name).CurrentValue = value;

                    entry.State = snapshot.State;
                    break;
            }
        }

        DetachEntitiesAddedByFailedMessage(changeTracker);
    }

    /// <summary>
    /// The entities the failed message added are not part of the snapshot, so
    /// they are detached here: the savepoint rollback already removed their
    /// rows, and they must not be inserted again.
    /// </summary>
    private void DetachEntitiesAddedByFailedMessage(ChangeTracker changeTracker)
    {
        var known = new HashSet<EntityEntry>(_entries.Select(e => e.Entry));

        foreach (var entry in changeTracker.Entries())
        {
            if (entry.State == EntityState.Added && !known.Contains(entry))
                entry.State = EntityState.Detached;
        }
    }

    private sealed record EntrySnapshot(
        EntityEntry Entry,
        EntityState State,
        Dictionary<string, object?>? Values);
}
