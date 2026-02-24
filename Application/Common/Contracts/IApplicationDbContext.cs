namespace Application.Common.Contracts;

public interface IApplicationDbContext
{
    DbSet<PaymentTransaction> PaymentTransactions { get; }
    DbSet<ElasticsearchOutboxMessage> ElasticsearchOutboxMessages { get; }
    DbSet<FailedElasticOperation> FailedElasticOperations { get; }
    DbSet<InventoryTransaction> InventoryTransactions { get; }

    Task<int> SaveChangesAsync(
        CancellationToken ct = default
        );
}