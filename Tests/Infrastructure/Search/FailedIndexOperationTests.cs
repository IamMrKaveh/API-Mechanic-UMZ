using Infrastructure.Search;

namespace Tests.Infrastructure.Search;

public class FailedIndexOperationTests
{
    [Fact]
    public void Defaults_AreNullWithZeroRetryCount()
    {
        var sut = new FailedIndexOperation();

        sut.EntityType.ShouldBeNull();
        sut.EntityId.ShouldBeNull();
        sut.Document.ShouldBeNull();
        sut.Error.ShouldBeNull();
        sut.Timestamp.ShouldBe(default);
        sut.RetryCount.ShouldBe(0);
    }

    [Fact]
    public void Properties_RoundtripAssignedValues()
    {
        var timestamp = new DateTime(2026, 4, 2, 8, 30, 0, DateTimeKind.Utc);

        var sut = new FailedIndexOperation
        {
            EntityType = "Product",
            EntityId = Guid.NewGuid().ToString(),
            Document = """{"name":"Brake Pad"}""",
            Error = "index rejected document",
            Timestamp = timestamp,
            RetryCount = 2
        };

        sut.EntityType.ShouldBe("Product");
        sut.EntityId.ShouldNotBeNullOrWhiteSpace();
        sut.Document.ShouldContain("Brake Pad");
        sut.Error.ShouldBe("index rejected document");
        sut.Timestamp.ShouldBe(timestamp);
        sut.RetryCount.ShouldBe(2);
    }

    [Theory]
    [InlineData("Product")]
    [InlineData("Category")]
    [InlineData("Brand")]
    public void EntityType_SupportsKnownSearchableTypes(string entityType)
    {
        var sut = new FailedIndexOperation { EntityType = entityType };

        sut.EntityType.ShouldBe(entityType);
    }

    [Fact]
    public void RetryCount_IsSetAtConstruction()
    {
        var firstAttempt = new FailedIndexOperation { RetryCount = 0 };
        var retried = new FailedIndexOperation
        {
            EntityType = firstAttempt.EntityType,
            EntityId = firstAttempt.EntityId,
            Document = firstAttempt.Document,
            Error = firstAttempt.Error,
            Timestamp = firstAttempt.Timestamp,
            RetryCount = firstAttempt.RetryCount + 1
        };

        firstAttempt.RetryCount.ShouldBe(0);
        retried.RetryCount.ShouldBe(1);
    }

    [Fact]
    public void Instances_WithIdenticalValues_HaveEqualMembers()
    {
        var timestamp = DateTime.UtcNow;

        var first = new FailedIndexOperation { EntityType = "Brand", EntityId = "1", Document = "{}", Error = "e", Timestamp = timestamp, RetryCount = 3 };
        var second = new FailedIndexOperation { EntityType = "Brand", EntityId = "1", Document = "{}", Error = "e", Timestamp = timestamp, RetryCount = 3 };

        second.EntityType.ShouldBe(first.EntityType);
        second.EntityId.ShouldBe(first.EntityId);
        second.Document.ShouldBe(first.Document);
        second.Error.ShouldBe(first.Error);
        second.Timestamp.ShouldBe(first.Timestamp);
        second.RetryCount.ShouldBe(first.RetryCount);
        second.ShouldNotBeSameAs(first);
    }
}
