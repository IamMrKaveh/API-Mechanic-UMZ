using Domain.Support.ValueObjects;
using SharedKernel.Abstractions;

namespace Tests.Domain.Support.ValueObjects;

public class TicketPriorityTests
{
    public static IEnumerable<object[]> AllPriorities =>
    [
        [TicketPriority.Low, "Low", "کم", 1],
        [TicketPriority.Normal, "Normal", "معمولی", 2],
        [TicketPriority.High, "High", "زیاد", 3],
        [TicketPriority.Urgent, "Urgent", "فوری", 4]
    ];

    [Theory]
    [MemberData(nameof(AllPriorities))]
    public void EachPriority_HasCorrectValueDisplayNameAndSortOrder(
        TicketPriority priority, string value, string displayName, int sortOrder)
    {
        priority.Value.ShouldBe(value);
        priority.DisplayName.ShouldBe(displayName);
        priority.SortOrder.ShouldBe(sortOrder);
    }

    [Theory]
    [InlineData("Low")]
    [InlineData("Normal")]
    [InlineData("High")]
    [InlineData("Urgent")]
    public void FromString_WithKnownValue_ReturnsMatchingPriority(string value)
    {
        TicketPriority.FromString(value).Value.ShouldBe(value);
    }

    [Theory]
    [InlineData("low", "Low")]
    [InlineData("NORMAL", "Normal")]
    [InlineData("hIgH", "High")]
    [InlineData("URGENT", "Urgent")]
    public void FromString_IsCaseInsensitive(string input, string expectedValue)
    {
        TicketPriority.FromString(input).Value.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromString_WithNullOrWhitespace_ReturnsNormalAsSafeDefault(string? input)
    {
        TicketPriority.FromString(input!).ShouldBe(TicketPriority.Normal);
    }

    [Fact]
    public void FromString_WithUnknownValue_ReturnsNormalAsSafeDefault()
    {
        TicketPriority.FromString("Extreme").ShouldBe(TicketPriority.Normal);
    }

    [Fact]
    public void FromString_WithKnownValue_ReturnsSameStaticInstance()
    {
        TicketPriority.FromString("High").ShouldBeSameAs(TicketPriority.High);
    }

    [Fact]
    public void Equality_ForSameValue_TreatsInstancesAsEqual()
    {
        TicketPriority.FromString("Urgent").ShouldBe(TicketPriority.Urgent);
    }

    [Fact]
    public void Equality_ForDifferentValues_TreatsInstancesAsUnequal()
    {
        TicketPriority.Low.ShouldNotBe(TicketPriority.High);
    }

    [Fact]
    public void IsAssignableToValueObjectBase()
    {
        TicketPriority.Normal.ShouldBeAssignableTo<ValueObject>();
    }
}
