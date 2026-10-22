using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence;

/// <summary>
/// Savepoint helpers for the ambient transaction. Savepoints are a relational
/// concept, so they are only available when a relational transaction is running.
/// </summary>
internal static class RelationalSavepointExtensions
{
    public static Task CreateSavepointAsync(
        this DatabaseFacade database,
        string name,
        CancellationToken ct = default)
    {
        var transaction = RequireRelationalTransaction(database);
        return transaction.CreateSavepointAsync(name, ct);
    }

    public static Task RollbackToSavepointAsync(
        this DatabaseFacade database,
        string name,
        CancellationToken ct = default)
    {
        var transaction = RequireRelationalTransaction(database);
        return transaction.RollbackToSavepointAsync(name, ct);
    }

    public static bool SupportsSavepoints(this DatabaseFacade database)
        => database.CurrentTransaction is RelationalTransaction;

    private static RelationalTransaction RequireRelationalTransaction(DatabaseFacade database)
        => database.CurrentTransaction as RelationalTransaction
           ?? throw new InvalidOperationException(
               "A savepoint was requested but the ambient transaction does not support them.");
}
