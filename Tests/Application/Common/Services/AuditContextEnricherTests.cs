using Application.Common.Services;

namespace Tests.Application.Common.Services;

public class AuditContextEnricherTests
{
    private readonly AuditContextEnricher _sut = new();

    [Fact]
    public void Set_ThenGet_ReturnsStoredValue()
    {
        _sut.Set("actorId", "42");

        _sut.Get("actorId").ShouldBe("42");
    }

    [Fact]
    public void Get_WhenKeyMissing_ReturnsNull()
    {
        _sut.Get("missing").ShouldBeNull();
    }

    [Fact]
    public void Set_WithNullValue_RemovesExistingKey()
    {
        _sut.Set("actorId", "42");
        _sut.Set("actorId", null);

        _sut.Get("actorId").ShouldBeNull();
    }

    [Fact]
    public void Set_WithNullValueForMissingKey_DoesNotThrow()
    {
        Should.NotThrow(() => _sut.Set("missing", null));

        _sut.Get("missing").ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Set_WithNullOrWhitespaceKey_IsIgnored(string? key)
    {
        _sut.Set(key!, "value");

        _sut.Snapshot().Count.ShouldBe(0);
    }

    [Fact]
    public void Set_IsCaseInsensitive_WhenReading()
    {
        _sut.Set("ActorId", "42");

        _sut.Get("actorid").ShouldBe("42");
        _sut.Get("ACTORID").ShouldBe("42");
    }

    [Fact]
    public void Set_SameKeyDifferentCasing_OverwritesExistingValue()
    {
        _sut.Set("actorId", "one");
        _sut.Set("ACTORID", "two");

        _sut.Snapshot().Count.ShouldBe(1);
        _sut.Get("actorId").ShouldBe("two");
    }

    [Fact]
    public void Snapshot_ReflectsCurrentValues()
    {
        _sut.Set("a", "1");
        _sut.Set("b", "2");

        var snapshot = _sut.Snapshot();

        snapshot.Count.ShouldBe(2);
        snapshot["a"].ShouldBe("1");
        snapshot["b"].ShouldBe("2");
    }

    [Fact]
    public void Clear_RemovesAllValues()
    {
        _sut.Set("a", "1");
        _sut.Set("b", "2");

        _sut.Clear();

        _sut.Snapshot().Count.ShouldBe(0);
        _sut.Get("a").ShouldBeNull();
    }
}
