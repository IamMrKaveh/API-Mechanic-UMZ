using Application.Order.Mapping;
using Domain.Order.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Application.Order.Mapping;

public class OrderStatusTransitionResolverTests
{
    [Theory]
    [InlineData("Paid", new[] { "Processing" })]
    [InlineData("Processing", new[] { "Shipped" })]
    [InlineData("Shipped", new[] { "Delivered" })]
    public void GetAllowedTransitions_ForTransitionalStatuses_ReturnsNextStep(string status, string[] expected)
    {
        var result = OrderStatusTransitionResolver.GetAllowedTransitions(OrderStatusValue.From(status));

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Created")]
    [InlineData("Reserved")]
    [InlineData("Pending")]
    [InlineData("Failed")]
    [InlineData("Delivered")]
    [InlineData("Cancelled")]
    [InlineData("Returned")]
    [InlineData("Refunded")]
    [InlineData("Expired")]
    public void GetAllowedTransitions_ForTerminalOrInitialStatuses_ReturnsEmpty(string status)
    {
        var result = OrderStatusTransitionResolver.GetAllowedTransitions(OrderStatusValue.From(status));

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public void GetAllowedTransitions_WhenStatusIsNull_ReturnsEmpty()
    {
        var result = OrderStatusTransitionResolver.GetAllowedTransitions(null!);

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public void GetAllowedTransitions_IsCaseInsensitive()
    {
        var lower = OrderStatusTransitionResolver.GetAllowedTransitions(OrderStatusValue.From("paid"));
        var upper = OrderStatusTransitionResolver.GetAllowedTransitions(OrderStatusValue.From("PAID"));

        lower.ShouldBe(new[] { "Processing" });
        upper.ShouldBe(new[] { "Processing" });
    }

    [Fact]
    public void GetAllowedTransitions_ForUnknownStatusValue_ThrowsFromFactory()
    {
        Should.Throw<DomainException>(() => OrderStatusValue.From("NonExistent"));
    }

    [Theory]
    [InlineData("Created")]
    [InlineData("Reserved")]
    [InlineData("Pending")]
    [InlineData("Failed")]
    [InlineData("Paid")]
    [InlineData("Processing")]
    [InlineData("Shipped")]
    [InlineData("Delivered")]
    [InlineData("Cancelled")]
    [InlineData("Returned")]
    [InlineData("Refunded")]
    [InlineData("Expired")]
    public void GetAllowedTransitions_ForEveryCanonicalStatus_DoesNotThrow(string status)
    {
        var result = OrderStatusTransitionResolver.GetAllowedTransitions(OrderStatusValue.From(status));

        result.ShouldNotBeNull();
    }
}
