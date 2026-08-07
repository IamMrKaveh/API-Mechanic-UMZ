using Domain.Order.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Order.ValueObjects;

public class OrderStatusValueTests
{
    public static IEnumerable<object[]> AllStatuses =>
    [
        [OrderStatusValue.Created, "Created", 0, false],
        [OrderStatusValue.Reserved, "Reserved", 1, false],
        [OrderStatusValue.Pending, "Pending", 2, false],
        [OrderStatusValue.Failed, "Failed", 3, false],
        [OrderStatusValue.Paid, "Paid", 4, false],
        [OrderStatusValue.Processing, "Processing", 5, false],
        [OrderStatusValue.Shipped, "Shipped", 6, false],
        [OrderStatusValue.Delivered, "Delivered", 7, true],
        [OrderStatusValue.Cancelled, "Cancelled", 8, true],
        [OrderStatusValue.Returned, "Returned", 9, true],
        [OrderStatusValue.Refunded, "Refunded", 10, true],
        [OrderStatusValue.Expired, "Expired", 11, true]
    ];

    [Theory]
    [MemberData(nameof(AllStatuses))]
    public void EachStatus_HasCorrectValueSortOrderAndFinalityFlag(
        OrderStatusValue status, string value, int sortOrder, bool isFinal)
    {
        status.Value.ShouldBe(value);
        status.SortOrder.ShouldBe(sortOrder);
        status.IsFinal.ShouldBe(isFinal);
    }

    [Fact]
    public void From_WithKnownValue_ReturnsMatchingStatus()
    {
        OrderStatusValue.From("Paid").ShouldBe(OrderStatusValue.Paid);
    }

    [Fact]
    public void From_IsCaseInsensitive()
    {
        OrderStatusValue.From("PAID").ShouldBe(OrderStatusValue.Paid);
        OrderStatusValue.From("paid").ShouldBe(OrderStatusValue.Paid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_WithNullOrWhitespace_ThrowsDomainException(string? input)
    {
        Should.Throw<DomainException>(() => OrderStatusValue.From(input!));
    }

    [Fact]
    public void From_WithUnknownValue_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => OrderStatusValue.From("Zorpified"));
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        string s = OrderStatusValue.Paid;

        s.ShouldBe("Paid");
    }

    [Theory]
    [InlineData("Paid", true)]
    [InlineData("Processing", true)]
    [InlineData("Shipped", true)]
    [InlineData("Delivered", true)]
    [InlineData("Created", false)]
    [InlineData("Reserved", false)]
    [InlineData("Pending", false)]
    [InlineData("Failed", false)]
    [InlineData("Cancelled", false)]
    [InlineData("Returned", false)]
    [InlineData("Refunded", false)]
    [InlineData("Expired", false)]
    public void IsPaid_ReturnsExpectedForEachStatus(string status, bool expected)
    {
        OrderStatusValue.From(status).IsPaid.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Created", true)]
    [InlineData("Reserved", true)]
    [InlineData("Pending", true)]
    [InlineData("Failed", true)]
    [InlineData("Paid", true)]
    [InlineData("Processing", true)]
    [InlineData("Shipped", false)]
    [InlineData("Delivered", false)]
    [InlineData("Cancelled", false)]
    [InlineData("Returned", false)]
    [InlineData("Refunded", false)]
    [InlineData("Expired", false)]
    public void CanBeCancelled_ReturnsExpectedForEachStatus(string status, bool expected)
    {
        OrderStatusValue.From(status).CanBeCancelled().ShouldBe(expected);
    }

    [Theory]
    [InlineData("Created", true)]
    [InlineData("Reserved", true)]
    [InlineData("Pending", true)]
    [InlineData("Failed", false)]
    [InlineData("Paid", false)]
    [InlineData("Processing", false)]
    [InlineData("Shipped", false)]
    [InlineData("Delivered", false)]
    [InlineData("Cancelled", false)]
    [InlineData("Returned", false)]
    [InlineData("Refunded", false)]
    [InlineData("Expired", false)]
    public void CanBeEdited_ReturnsExpectedForEachStatus(string status, bool expected)
    {
        OrderStatusValue.From(status).CanBeEdited().ShouldBe(expected);
    }

    [Theory]
    [InlineData("Created", "Reserved")]
    [InlineData("Created", "Pending")]
    [InlineData("Created", "Paid")]
    [InlineData("Created", "Cancelled")]
    [InlineData("Created", "Expired")]
    [InlineData("Reserved", "Pending")]
    [InlineData("Reserved", "Cancelled")]
    [InlineData("Reserved", "Expired")]
    [InlineData("Pending", "Paid")]
    [InlineData("Pending", "Failed")]
    [InlineData("Pending", "Cancelled")]
    [InlineData("Pending", "Expired")]
    [InlineData("Failed", "Pending")]
    [InlineData("Failed", "Cancelled")]
    [InlineData("Failed", "Expired")]
    [InlineData("Paid", "Processing")]
    [InlineData("Paid", "Cancelled")]
    [InlineData("Paid", "Refunded")]
    [InlineData("Processing", "Shipped")]
    [InlineData("Processing", "Cancelled")]
    [InlineData("Shipped", "Delivered")]
    [InlineData("Shipped", "Returned")]
    [InlineData("Delivered", "Returned")]
    [InlineData("Delivered", "Refunded")]
    [InlineData("Returned", "Refunded")]
    public void CanTransitionTo_AllowedTransition_ReturnsTrue(string from, string to)
    {
        OrderStatusValue.From(from).CanTransitionTo(OrderStatusValue.From(to)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Created", "Processing")]
    [InlineData("Created", "Shipped")]
    [InlineData("Reserved", "Paid")]
    [InlineData("Pending", "Processing")]
    [InlineData("Paid", "Delivered")]
    [InlineData("Processing", "Refunded")]
    [InlineData("Shipped", "Cancelled")]
    [InlineData("Delivered", "Cancelled")]
    public void CanTransitionTo_DisallowedTransition_ReturnsFalse(string from, string to)
    {
        OrderStatusValue.From(from).CanTransitionTo(OrderStatusValue.From(to)).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Cancelled")]
    [InlineData("Refunded")]
    [InlineData("Expired")]
    public void TerminalStatuses_HaveNoAllowedTransitions(string status)
    {
        var from = OrderStatusValue.From(status);

        from.CanTransitionTo(OrderStatusValue.Created).ShouldBeFalse();
        from.CanTransitionTo(OrderStatusValue.Paid).ShouldBeFalse();
        from.CanTransitionTo(OrderStatusValue.Processing).ShouldBeFalse();
        from.CanTransitionTo(OrderStatusValue.Shipped).ShouldBeFalse();
        from.CanTransitionTo(OrderStatusValue.Delivered).ShouldBeFalse();
        from.CanTransitionTo(OrderStatusValue.Refunded).ShouldBeFalse();
    }
}
