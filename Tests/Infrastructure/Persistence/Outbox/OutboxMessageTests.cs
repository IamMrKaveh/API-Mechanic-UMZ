using Infrastructure.Persistence.Outbox;

namespace Tests.Infrastructure.Persistence.Outbox;

public class OutboxMessageTests
{
    private const string ValidType = "SampleEvent"; private const string ValidPayload = """{"key":"value"}"""; private static readonly DateTime FixedCreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidArguments_ProducesMessageWithProvidedValues()
    {
        var message = OutboxMessage.Create(ValidType, ValidPayload, FixedCreatedAt);

        message.Id.Value.ShouldNotBe(Guid.Empty);
        message.Type.ShouldBe(ValidType);
        message.Payload.ShouldBe(ValidPayload);
        message.CreatedAt.ShouldBe(FixedCreatedAt);
        message.ProcessedAt.ShouldBeNull();
        message.Error.ShouldBeNull();
        message.RetryCount.ShouldBe(0);
        message.IsPoisoned.ShouldBeFalse();
        message.TraceParent.ShouldBeNull();
        message.TraceState.ShouldBeNull();
    }

    [Fact]
    public void Create_WithTraceInformation_CopiesTraceValuesToMessage()
    {
        var message = OutboxMessage.Create(ValidType, ValidPayload, FixedCreatedAt, "trace-parent", "trace-state");

        message.TraceParent.ShouldBe("trace-parent");
        message.TraceState.ShouldBe("trace-state");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankType_ThrowsArgumentException(string type)
    {
        Should.Throw<ArgumentException>(() =>
            OutboxMessage.Create(type, ValidPayload, FixedCreatedAt));
    }

    [Fact]
    public void Create_WithNullType_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            OutboxMessage.Create(null!, ValidPayload, FixedCreatedAt));
    }

    [Fact]
    public void Create_WithNullPayload_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            OutboxMessage.Create(ValidType, null!, FixedCreatedAt));
    }

    [Fact]
    public void Create_WithPayloadExceedingSixtyFourKilobytes_ThrowsInvalidOperationException()
    {
        var oversized = new string('a', 64 * 1024 + 1);

        Should.Throw<InvalidOperationException>(() =>
            OutboxMessage.Create(ValidType, oversized, FixedCreatedAt));
    }

    [Fact]
    public void Create_WithPayloadExactlyAtSixtyFourKilobytes_Succeeds()
    {
        var atLimit = new string('a', 64 * 1024);

        var message = OutboxMessage.Create(ValidType, atLimit, FixedCreatedAt);

        Encoding.UTF8.GetByteCount(message.Payload).ShouldBe(64 * 1024);
    }

    [Fact]
    public void MarkProcessed_WithProcessedAt_SetsProcessedAtAndClearsError()
    {
        var message = OutboxMessage.Create(ValidType, ValidPayload, FixedCreatedAt);
        message.MarkFailed("temporary");

        var processedAt = new DateTime(2026, 5, 5, 12, 0, 0, DateTimeKind.Utc);
        message.MarkProcessed(processedAt);

        message.ProcessedAt.ShouldBe(processedAt);
        message.Error.ShouldBeNull();
    }

    [Fact]
    public void MarkFailed_WithError_IncrementsRetryCountAndSetsError()
    {
        var message = OutboxMessage.Create(ValidType, ValidPayload, FixedCreatedAt);

        message.MarkFailed("first failure");
        message.MarkFailed("second failure");

        message.RetryCount.ShouldBe(2);
        message.Error.ShouldBe("second failure");
        message.IsPoisoned.ShouldBeFalse();
        message.ProcessedAt.ShouldBeNull();
    }

    [Fact]
    public void MarkPoisoned_WithError_SetsPoisonedFlagAndError()
    {
        var message = OutboxMessage.Create(ValidType, ValidPayload, FixedCreatedAt);

        message.MarkPoisoned("fatal");

        message.IsPoisoned.ShouldBeTrue();
        message.Error.ShouldBe("fatal");
    }

    [Fact]
    public void Create_TwoInvocations_ProduceDistinctIds()
    {
        var a = OutboxMessage.Create(ValidType, ValidPayload, FixedCreatedAt);
        var b = OutboxMessage.Create(ValidType, ValidPayload, FixedCreatedAt);

        a.Id.ShouldNotBe(b.Id);
    }
}
