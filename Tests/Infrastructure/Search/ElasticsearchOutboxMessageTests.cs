using Infrastructure.Search;

namespace Tests.Infrastructure.Search;

public class ElasticsearchOutboxMessageTests
{
    [Fact]
    public void Create_WithValidInputs_InitializesIdentityAndDefaults()
    {
        var entityId = Guid.NewGuid(); var before = DateTime.UtcNow.AddSeconds(-1);

        var message = ElasticsearchOutboxMessage.Create("Product", entityId, "{}", "Upsert");

        var after = DateTime.UtcNow.AddSeconds(1);

        message.Id.ShouldNotBe(Guid.Empty);
        message.EntityType.ShouldBe("Product");
        message.EntityId.ShouldBe(entityId);
        message.Document.ShouldBe("{}");
        message.ChangeType.ShouldBe("Upsert");
        message.RetryCount.ShouldBe(0);
        message.ProcessedAt.ShouldBeNull();
        message.Error.ShouldBeNull();
        message.IsPoisoned.ShouldBeFalse();
        message.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        message.CreatedAt.ShouldBeLessThanOrEqualTo(after);
        message.NextAttemptAt.ShouldNotBeNull();
        message.NextAttemptAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
        message.NextAttemptAt!.Value.ShouldBeLessThanOrEqualTo(after);
    }

    [Theory]
    [InlineData("Product", "Upsert")]
    [InlineData("Category", "Delete")]
    [InlineData("Brand", "Upsert")]
    public void Create_WithValidInputs_ProducesDeterministicIdempotencyKey(string entityType, string changeType)
    {
        var entityId = Guid.NewGuid();

        var message = ElasticsearchOutboxMessage.Create(entityType, entityId, "{}", changeType);

        message.IdempotencyKey.ShouldBe($"{entityType}:{entityId}:{changeType}");
    }

    [Fact]
    public void Create_TwoInvocationsWithSameInputs_ProduceDistinctIds()
    {
        var entityId = Guid.NewGuid();

        var first = ElasticsearchOutboxMessage.Create("Product", entityId, "{}", "Upsert");
        var second = ElasticsearchOutboxMessage.Create("Product", entityId, "{}", "Upsert");

        first.Id.ShouldNotBe(second.Id);
    }

    [Fact]
    public void Create_TwoInvocationsWithSameInputs_ProduceSameIdempotencyKey()
    {
        var entityId = Guid.NewGuid();

        var first = ElasticsearchOutboxMessage.Create("Product", entityId, "{}", "Upsert");
        var second = ElasticsearchOutboxMessage.Create("Product", entityId, "{}", "Upsert");

        first.IdempotencyKey.ShouldBe(second.IdempotencyKey);
    }

    [Fact]
    public void MarkProcessed_AfterCreate_SetsProcessedAtAndClearsErrorAndNextAttemptAt()
    {
        var message = ElasticsearchOutboxMessage.Create("Product", Guid.NewGuid(), "{}", "Upsert");
        message.MarkFailed("transient failure", TimeSpan.FromSeconds(5));

        var before = DateTime.UtcNow.AddSeconds(-1);
        message.MarkProcessed();
        var after = DateTime.UtcNow.AddSeconds(1);

        message.ProcessedAt.ShouldNotBeNull();
        message.ProcessedAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
        message.ProcessedAt!.Value.ShouldBeLessThanOrEqualTo(after);
        message.Error.ShouldBeNull();
        message.NextAttemptAt.ShouldBeNull();
    }

    [Fact]
    public void MarkProcessed_DoesNotChangeRetryCountOrPoisonedFlag()
    {
        var message = ElasticsearchOutboxMessage.Create("Product", Guid.NewGuid(), "{}", "Upsert");
        message.MarkFailed("transient", TimeSpan.FromSeconds(1));
        var retryBefore = message.RetryCount;

        message.MarkProcessed();

        message.RetryCount.ShouldBe(retryBefore);
        message.IsPoisoned.ShouldBeFalse();
    }

    [Fact]
    public void MarkFailed_IncrementsRetryCountAndSetsErrorAndSchedulesNextAttempt()
    {
        var message = ElasticsearchOutboxMessage.Create("Product", Guid.NewGuid(), "{}", "Upsert");
        var delay = TimeSpan.FromMinutes(10);
        var before = DateTime.UtcNow.Add(delay).AddSeconds(-2);

        message.MarkFailed("boom", delay);

        var after = DateTime.UtcNow.Add(delay).AddSeconds(2);
        message.RetryCount.ShouldBe(1);
        message.Error.ShouldBe("boom");
        message.NextAttemptAt.ShouldNotBeNull();
        message.NextAttemptAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
        message.NextAttemptAt!.Value.ShouldBeLessThanOrEqualTo(after);
        message.ProcessedAt.ShouldBeNull();
        message.IsPoisoned.ShouldBeFalse();
    }

    [Fact]
    public void MarkFailed_CalledMultipleTimes_AccumulatesRetryCount()
    {
        var message = ElasticsearchOutboxMessage.Create("Product", Guid.NewGuid(), "{}", "Upsert");

        message.MarkFailed("first", TimeSpan.FromSeconds(1));
        message.MarkFailed("second", TimeSpan.FromSeconds(2));
        message.MarkFailed("third", TimeSpan.FromSeconds(3));

        message.RetryCount.ShouldBe(3);
        message.Error.ShouldBe("third");
    }

    [Fact]
    public void MarkPoisoned_SetsIsPoisonedAndErrorAndClearsNextAttemptAt()
    {
        var message = ElasticsearchOutboxMessage.Create("Product", Guid.NewGuid(), "{}", "Upsert");
        message.MarkFailed("transient", TimeSpan.FromMinutes(1));

        message.MarkPoisoned("permanent");

        message.IsPoisoned.ShouldBeTrue();
        message.Error.ShouldBe("permanent");
        message.NextAttemptAt.ShouldBeNull();
    }

    [Fact]
    public void MarkPoisoned_DoesNotResetRetryCountOrProcessedAt()
    {
        var message = ElasticsearchOutboxMessage.Create("Product", Guid.NewGuid(), "{}", "Upsert");
        message.MarkFailed("transient", TimeSpan.FromSeconds(1));
        var retryBefore = message.RetryCount;
        var processedBefore = message.ProcessedAt;

        message.MarkPoisoned("permanent");

        message.RetryCount.ShouldBe(retryBefore);
        message.ProcessedAt.ShouldBe(processedBefore);
    }

    [Fact]
    public void IncrementRetry_WithError_IncrementsRetryCountAndSetsProvidedError()
    {
        var message = ElasticsearchOutboxMessage.Create("Product", Guid.NewGuid(), "{}", "Upsert");

        message.IncrementRetry("something went wrong");

        message.RetryCount.ShouldBe(1);
        message.Error.ShouldBe("something went wrong");
    }

    [Fact]
    public void IncrementRetry_WithoutError_IncrementsRetryCountAndClearsError()
    {
        var message = ElasticsearchOutboxMessage.Create("Product", Guid.NewGuid(), "{}", "Upsert");
        message.MarkFailed("previous", TimeSpan.FromSeconds(1));

        message.IncrementRetry();

        message.RetryCount.ShouldBe(2);
        message.Error.ShouldBeNull();
    }
}
