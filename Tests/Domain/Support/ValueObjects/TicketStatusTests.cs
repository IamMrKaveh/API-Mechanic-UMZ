using Domain.Support.ValueObjects;
using SharedKernel.Abstractions;

namespace Tests.Domain.Support.ValueObjects;

public class TicketStatusTests
{
    public static IEnumerable<object[]> AllStatuses =>
    [
        [TicketStatus.Open, "Open", "باز", false, true, 1],
        [TicketStatus.AwaitingReply, "AwaitingReply", "در انتظار پاسخ", false, true, 2],
        [TicketStatus.Answered, "Answered", "پاسخ داده شده", false, false, 3],
        [TicketStatus.Closed, "Closed", "بسته شده", true, false, 4]
    ];

    [Theory]
    [MemberData(nameof(AllStatuses))]
    public void EachStatus_HasCorrectValueDisplayNameFlagsAndSortOrder(
        TicketStatus status,
        string value,
        string displayName,
        bool isClosed,
        bool requiresResponse,
        int sortOrder)
    {
        status.Value.ShouldBe(value);
        status.DisplayName.ShouldBe(displayName);
        status.IsClosed.ShouldBe(isClosed);
        status.RequiresResponse.ShouldBe(requiresResponse);
        status.SortOrder.ShouldBe(sortOrder);
    }

    [Theory]
    [InlineData("Open")]
    [InlineData("AwaitingReply")]
    [InlineData("Answered")]
    [InlineData("Closed")]
    public void FromString_WithKnownValue_ReturnsMatchingStatus(string value)
    {
        TicketStatus.FromString(value).Value.ShouldBe(value);
    }

    [Theory]
    [InlineData("open", "Open")]
    [InlineData("AWAITINGREPLY", "AwaitingReply")]
    [InlineData("answered", "Answered")]
    [InlineData("CLOSED", "Closed")]
    public void FromString_IsCaseInsensitive(string input, string expectedValue)
    {
        TicketStatus.FromString(input).Value.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromString_WithNullOrWhitespace_ReturnsOpenAsSafeDefault(string? input)
    {
        TicketStatus.FromString(input!).ShouldBe(TicketStatus.Open);
    }

    [Fact]
    public void FromString_WithUnknownValue_ReturnsOpenAsSafeDefault()
    {
        TicketStatus.FromString("Frozen").ShouldBe(TicketStatus.Open);
    }

    [Fact]
    public void FromString_WithKnownValue_ReturnsSameStaticInstance()
    {
        TicketStatus.FromString("Closed").ShouldBeSameAs(TicketStatus.Closed);
    }

    [Fact]
    public void Equality_ForSameValue_TreatsInstancesAsEqual()
    {
        TicketStatus.FromString("Answered").ShouldBe(TicketStatus.Answered);
    }

    [Fact]
    public void Equality_ForDifferentValues_TreatsInstancesAsUnequal()
    {
        TicketStatus.Open.ShouldNotBe(TicketStatus.Closed);
    }

    [Fact]
    public void IsAssignableToValueObjectBase()
    {
        TicketStatus.Open.ShouldBeAssignableTo<ValueObject>();
    }
}
