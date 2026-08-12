using Domain.Notification.ValueObjects;

namespace Tests.Domain.Notification.ValueObjects;

public class NotificationPriorityTests
{
    public static IEnumerable<object[]> CatalogEntries()
    {
        yield return new object[] { NotificationPriority.Low, "Low", "کم", 1 };
        yield return new object[] { NotificationPriority.Normal, "Normal", "معمولی", 2 };
        yield return new object[] { NotificationPriority.High, "High", "زیاد", 3 };
        yield return new object[] { NotificationPriority.Urgent, "Urgent", "فوری", 4 };
    }

    [Theory]
    [MemberData(nameof(CatalogEntries))]
    public void CatalogEntry_ExposesExpectedValueDisplayNameAndSortOrder(
        NotificationPriority sut,
        string expectedValue,
        string expectedDisplayName,
        int expectedSortOrder)
    {
        sut.Value.ShouldBe(expectedValue);
        sut.DisplayName.ShouldBe(expectedDisplayName);
        sut.SortOrder.ShouldBe(expectedSortOrder);
    }

    [Fact]
    public void SortOrder_IsStrictlyIncreasingFromLowToUrgent()
    {
        NotificationPriority.Low.SortOrder.ShouldBeLessThan(NotificationPriority.Normal.SortOrder);
        NotificationPriority.Normal.SortOrder.ShouldBeLessThan(NotificationPriority.High.SortOrder);
        NotificationPriority.High.SortOrder.ShouldBeLessThan(NotificationPriority.Urgent.SortOrder);
    }

    [Fact]
    public void Equality_ForSameValue_TreatsInstancesAsEqual()
    {
        NotificationPriority.High.ShouldBe(NotificationPriority.High);
    }

    [Fact]
    public void Equality_ForDifferentValues_TreatsInstancesAsNotEqual()
    {
        NotificationPriority.Low.ShouldNotBe(NotificationPriority.Urgent);
    }

    [Fact]
    public void ImplicitConversionToString_ReturnsValue()
    {
        string asString = NotificationPriority.Urgent;

        asString.ShouldBe("Urgent");
    }
}
