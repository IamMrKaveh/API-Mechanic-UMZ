namespace Infrastructure.Persistence.Outbox;

public interface IOutboxProcessor
{
    Task ProcessAsync(int batchSize = 50, CancellationToken ct = default);
}