using Infrastructure.Persistence.Outbox;

namespace Tests.Infrastructure.Persistence.Outbox;

public class OutboxArchiveMessageTests
{
    private const string ValidType = "SampleEvent"; private const string ValidPayload = """{"key":"value"}"""; private static readonly DateTime FixedCreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc); private static readonly DateTime FixedProcessedAt = new(2026, 1, 1, 1, 0, 0, DateTimeKind.Utc); private static readonly DateTime FixedArchivedAt = new(2026, 1, 1, 2, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FromProcessed_WithNullSource_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            OutboxArchiveMessage.FromProcessed(null!, FixedArchivedAt));
    }

    [Fact]
    public void FromProcessed_WithUnprocessedMessage_ThrowsInvalidOperationException()
    {
        var pending = OutboxMessage.Create(ValidType, ValidPayload, FixedCreatedAt);

        Should.Throw<InvalidOperationException>(() =>
            OutboxArchiveMessage.FromProcessed(pending, FixedArchivedAt));
    }

    [Fact]
    public void FromProcessed_WithProcessedMessage_CopiesAllFieldsAndSetsArchivedAt()
    {
        var source = OutboxMessage.Create(
            ValidType,
            ValidPayload,
            FixedCreatedAt,
            traceParent: "trace-parent",
            traceState: "trace-state");
        source.MarkFailed("previous failure");
        source.MarkProcessed(FixedProcessedAt);

        var archive = OutboxArchiveMessage.FromProcessed(source, FixedArchivedAt);

        archive.Id.ShouldBe(source.Id);
        archive.Type.ShouldBe(source.Type);
        archive.Payload.ShouldBe(source.Payload);
        archive.CreatedAt.ShouldBe(source.CreatedAt);
        archive.ProcessedAt.ShouldBe(FixedProcessedAt);
        archive.Error.ShouldBeNull();
        archive.RetryCount.ShouldBe(source.RetryCount);
        archive.IsPoisoned.ShouldBe(source.IsPoisoned);
        archive.TraceParent.ShouldBe(source.TraceParent);
        archive.TraceState.ShouldBe(source.TraceState);
        archive.ArchivedAt.ShouldBe(FixedArchivedAt);
    }

    [Fact]
    public void FromProcessed_WithPoisonedProcessedMessage_PreservesPoisonedFlagAndError()
    {
        var source = OutboxMessage.Create(ValidType, ValidPayload, FixedCreatedAt);
        source.MarkPoisoned("fatal");
        source.MarkProcessed(FixedProcessedAt);

        var archive = OutboxArchiveMessage.FromProcessed(source, FixedArchivedAt);

        archive.IsPoisoned.ShouldBeTrue();
        archive.ProcessedAt.ShouldBe(FixedProcessedAt);
    }
}
