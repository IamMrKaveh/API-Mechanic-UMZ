using Domain.Order.Events;
using Domain.Order.Exceptions;
using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Order.Aggregates;

public class OrderTests
{
    [Fact]
    public void Place_WithValidInput_ReturnsInitializedOrderWithStatusCreated()
    {
        var orderId = OrderId.NewId();
        var userId = UserId.NewId();
        var idempotencyKey = Guid.NewGuid();

        var sut = new OrderBuilder()
            .WithOrderId(orderId)
            .WithUserId(userId)
            .WithIdempotencyKey(idempotencyKey)
            .Build();

        sut.Id.ShouldBe(orderId);
        sut.UserId.ShouldBe(userId);
        sut.IdempotencyKey.ShouldBe(idempotencyKey);
        sut.Status.ShouldBe(OrderStatusValue.Created);
        sut.IsPaid.ShouldBeFalse();
        sut.IsCancelled.ShouldBeFalse();
        sut.IsDelivered.ShouldBeFalse();
        sut.IsShipped.ShouldBeFalse();
        sut.IsDeleted.ShouldBeFalse();
        sut.UpdatedAt.ShouldBeNull();
        sut.OrderItems.Count.ShouldBe(1);
    }

    [Fact]
    public void Place_SetsCreatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new OrderBuilder().Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Place_ProducesOrderWithVersionOne()
    {
        new OrderBuilder().Build().Version.ShouldBe(1);
    }

    [Fact]
    public void Place_RaisesExactlyOneOrderCreatedEvent()
    {
        var sut = new OrderBuilder().Build();

        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<OrderCreatedEvent>();
    }

    [Fact]
    public void Place_GeneratesOrderNumberMatchingDateSegment()
    {
        var sut = new OrderBuilder().WithOrderDate(new DateOnly(2026, 8, 4)).Build();

        sut.OrderNumber.Value.ShouldStartWith("ORD-20260804-");
    }

    [Fact]
    public void Place_ComputesSubTotalAsSumOfItemTotalPrices()
    {
        var item1 = new OrderItemSnapshotBuilder().WithUnitPrice(100m, "IRT").WithQuantity(2).Build();
        var item2 = new OrderItemSnapshotBuilder().WithUnitPrice(50m, "IRT").WithQuantity(3).Build();

        var sut = new OrderBuilder().WithItemSnapshots(item1, item2).Build();

        sut.SubTotal.Amount.ShouldBe(350m);
    }

    [Fact]
    public void Place_ComputesFinalAmountAsSubTotalPlusShippingMinusDiscount()
    {
        var item = new OrderItemSnapshotBuilder().WithUnitPrice(100m, "IRT").WithQuantity(2).Build();

        var sut = new OrderBuilder()
            .WithItemSnapshots(item)
            .WithShippingCost(50m, "IRT")
            .WithDiscountAmount(30m, "IRT")
            .Build();

        sut.SubTotal.Amount.ShouldBe(200m);
        sut.FinalAmount.Amount.ShouldBe(220m);
    }

    [Fact]
    public void Place_WhenDiscountExceedsBeforeDiscountTotal_ClampsFinalAmountAtZeroInSubTotalCurrency()
    {
        var item = new OrderItemSnapshotBuilder().WithUnitPrice(50m, "IRT").WithQuantity(1).Build();

        var sut = new OrderBuilder()
            .WithItemSnapshots(item)
            .WithShippingCost(20m, "IRT")
            .WithDiscountAmount(500m, "IRT")
            .Build();

        sut.FinalAmount.Amount.ShouldBe(0m);
        sut.FinalAmount.Currency.ShouldBe("IRT");
    }

    [Fact]
    public void Place_OrderCreatedEventCarriesPostDiscountFinalAmount()
    {
        var item = new OrderItemSnapshotBuilder().WithUnitPrice(100m, "IRT").WithQuantity(2).Build();

        var sut = new OrderBuilder()
            .WithItemSnapshots(item)
            .WithShippingCost(50m, "IRT")
            .WithDiscountAmount(30m, "IRT")
            .Build();

        var evt = sut.DomainEvents.Single().ShouldBeOfType<OrderCreatedEvent>();
        evt.FinalAmount.ShouldBe(220m);
        evt.Currency.ShouldBe("IRT");
        evt.ItemsCount.ShouldBe(1);
    }

    [Fact]
    public void Place_WithNoItems_ThrowsEmptyOrderException()
    {
        Should.Throw<EmptyOrderException>(() => new OrderBuilder().WithNoItems().Build());
    }

    [Fact]
    public void Place_WithNullUserId_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new OrderBuilder().WithUserId(null!).Build());
    }

    [Fact]
    public void Place_WithEmptyIdempotencyKey_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new OrderBuilder().WithIdempotencyKey(Guid.Empty).Build());
    }

    [Fact]
    public void Place_WithPaymentMethodId_AssignsItToOrder()
    {
        var paymentMethodId = PaymentMethodId.NewId();

        var sut = new OrderBuilder().WithPaymentMethodId(paymentMethodId).Build();

        sut.PaymentMethodId.ShouldBe(paymentMethodId);
    }

    [Fact]
    public void Place_WithoutPaymentMethodId_LeavesPaymentMethodIdNull()
    {
        new OrderBuilder().Build().PaymentMethodId.ShouldBeNull();
    }

    [Fact]
    public void AssignPaymentMethod_OnUnpaidOrder_SetsPaymentMethodIdWithoutRaisingEvent()
    {
        var sut = new OrderBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;
        var newPaymentMethod = PaymentMethodId.NewId();

        sut.AssignPaymentMethod(newPaymentMethod);

        sut.PaymentMethodId.ShouldBe(newPaymentMethod);
        sut.UpdatedAt.ShouldNotBeNull();
        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void AssignPaymentMethod_OnPaidOrder_ThrowsDomainException()
    {
        var sut = new OrderBuilder().Build();
        sut.MoveToPending();
        sut.MarkAsPaid(PaymentTransactionId.NewId());

        Should.Throw<DomainException>(() => sut.AssignPaymentMethod(PaymentMethodId.NewId()));
    }

    [Fact]
    public void AssignPaymentMethod_WithNull_ThrowsArgumentException()
    {
        var sut = new OrderBuilder().Build();

        Should.Throw<ArgumentException>(() => sut.AssignPaymentMethod(null!));
    }

    [Fact]
    public void MoveToPending_FromCreated_TransitionsToPendingAndRaisesEvent()
    {
        var sut = new OrderBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.MoveToPending();

        sut.Status.ShouldBe(OrderStatusValue.Pending);
        sut.Version.ShouldBe(versionBefore + 1);
        sut.DomainEvents.Single().ShouldBeOfType<OrderStatusChangedEvent>();
    }

    [Fact]
    public void MoveToPending_WhenAlreadyPending_IsIdempotentNoOp()
    {
        var sut = new OrderBuilder().Build();
        sut.MoveToPending();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.MoveToPending();

        sut.Status.ShouldBe(OrderStatusValue.Pending);
        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void MarkAsPaid_FromPending_TransitionsToPaidAndRaisesTwoEvents()
    {
        var sut = new OrderBuilder().Build();
        sut.MoveToPending();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;
        var paymentTxId = PaymentTransactionId.NewId();

        sut.MarkAsPaid(paymentTxId);

        sut.Status.ShouldBe(OrderStatusValue.Paid);
        sut.PaymentTransactionId.ShouldBe(paymentTxId);
        sut.IsPaid.ShouldBeTrue();
        sut.Version.ShouldBe(versionBefore + 2);
        sut.DomainEvents.Count.ShouldBe(2);
        sut.DomainEvents.ElementAt(0).ShouldBeOfType<OrderStatusChangedEvent>();
        sut.DomainEvents.ElementAt(1).ShouldBeOfType<OrderPaidEvent>();
    }

    [Fact]
    public void MarkAsPaid_DirectlyFromCreated_IsAllowed()
    {
        var sut = new OrderBuilder().Build();

        Should.NotThrow(() => sut.MarkAsPaid(PaymentTransactionId.NewId()));
        sut.Status.ShouldBe(OrderStatusValue.Paid);
    }

    [Fact]
    public void MarkAsPaid_WithNullPaymentTransactionId_ThrowsArgumentNullException()
    {
        var sut = new OrderBuilder().Build();

        Should.Throw<ArgumentNullException>(() => sut.MarkAsPaid(null!));
    }

    [Fact]
    public void MarkAsPaid_FromCancelled_ThrowsInvalidOrderTransitionException()
    {
        var sut = new OrderBuilder().Build();
        sut.Cancel("changed mind");

        Should.Throw<InvalidOrderTransitionException>(() => sut.MarkAsPaid(PaymentTransactionId.NewId()));
    }

    [Fact]
    public void StartProcessing_FromPaid_TransitionsToProcessing()
    {
        var sut = new OrderBuilder().Build();
        sut.MarkAsPaid(PaymentTransactionId.NewId());
        sut.ClearDomainEvents();

        sut.StartProcessing();

        sut.Status.ShouldBe(OrderStatusValue.Processing);
        sut.DomainEvents.Single().ShouldBeOfType<OrderStatusChangedEvent>();
    }

    [Fact]
    public void StartProcessing_FromPending_ThrowsInvalidOrderTransitionException()
    {
        var sut = new OrderBuilder().Build();
        sut.MoveToPending();

        Should.Throw<InvalidOrderTransitionException>(sut.StartProcessing);
    }

    [Fact]
    public void MarkAsShipped_FromProcessing_TransitionsToShipped()
    {
        var sut = new OrderBuilder().Build();
        sut.MarkAsPaid(PaymentTransactionId.NewId());
        sut.StartProcessing();

        sut.MarkAsShipped();

        sut.Status.ShouldBe(OrderStatusValue.Shipped);
        sut.IsShipped.ShouldBeTrue();
    }

    [Fact]
    public void MarkAsDelivered_FromShipped_TransitionsToDelivered()
    {
        var sut = new OrderBuilder().Build();
        sut.MarkAsPaid(PaymentTransactionId.NewId());
        sut.StartProcessing();
        sut.MarkAsShipped();

        sut.MarkAsDelivered();

        sut.Status.ShouldBe(OrderStatusValue.Delivered);
        sut.IsDelivered.ShouldBeTrue();
    }

    [Fact]
    public void Cancel_FromCreatedWithValidReason_TransitionsToCancelledAndRaisesTwoEvents()
    {
        var sut = new OrderBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Cancel("customer changed mind");

        sut.Status.ShouldBe(OrderStatusValue.Cancelled);
        sut.IsCancelled.ShouldBeTrue();
        sut.CancellationReason.ShouldBe("customer changed mind");
        sut.Version.ShouldBe(versionBefore + 2);
        sut.DomainEvents.Count.ShouldBe(2);
        sut.DomainEvents.ElementAt(0).ShouldBeOfType<OrderStatusChangedEvent>();
        var cancelledEvt = sut.DomainEvents.ElementAt(1).ShouldBeOfType<OrderCancelledEvent>();
        cancelledEvt.WasPaid.ShouldBeFalse();
    }

    [Fact]
    public void Cancel_FromPaid_CapturesWasPaidTrueOnEvent()
    {
        var sut = new OrderBuilder().Build();
        sut.MarkAsPaid(PaymentTransactionId.NewId());
        sut.ClearDomainEvents();

        sut.Cancel("refund requested");

        var evt = sut.DomainEvents.OfType<OrderCancelledEvent>().Single();
        evt.WasPaid.ShouldBeTrue();
    }

    [Fact]
    public void Cancel_FromShipped_ThrowsOrderCancellationNotAllowedException()
    {
        var sut = new OrderBuilder().Build();
        sut.MarkAsPaid(PaymentTransactionId.NewId());
        sut.StartProcessing();
        sut.MarkAsShipped();

        Should.Throw<OrderCancellationNotAllowedException>(() => sut.Cancel("too late"));
    }

    [Fact]
    public void Cancel_FromDelivered_ThrowsOrderCancellationNotAllowedException()
    {
        var sut = new OrderBuilder().Build();
        sut.MarkAsPaid(PaymentTransactionId.NewId());
        sut.StartProcessing();
        sut.MarkAsShipped();
        sut.MarkAsDelivered();

        Should.Throw<OrderCancellationNotAllowedException>(() => sut.Cancel("too late"));
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ThrowsOrderCancellationNotAllowedException()
    {
        var sut = new OrderBuilder().Build();
        sut.Cancel("first");

        Should.Throw<OrderCancellationNotAllowedException>(() => sut.Cancel("second"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Cancel_WithNullOrWhitespaceReason_ThrowsArgumentException(string? reason)
    {
        var sut = new OrderBuilder().Build();

        Should.Throw<ArgumentException>(() => sut.Cancel(reason!));
    }

    [Fact]
    public void Expire_OnUnpaidCreatedOrder_TransitionsToExpired()
    {
        var sut = new OrderBuilder().Build();

        sut.Expire(OrderStatusValue.Expired);

        sut.Status.ShouldBe(OrderStatusValue.Expired);
    }

    [Fact]
    public void Expire_OnPaidOrder_ThrowsInvalidOrderTransitionException()
    {
        var sut = new OrderBuilder().Build();
        sut.MarkAsPaid(PaymentTransactionId.NewId());

        Should.Throw<InvalidOrderTransitionException>(() => sut.Expire(OrderStatusValue.Expired));
    }

    [Fact]
    public void Refund_FromPaid_TransitionsToRefunded()
    {
        var sut = new OrderBuilder().Build();
        sut.MarkAsPaid(PaymentTransactionId.NewId());

        sut.Refund();

        sut.Status.ShouldBe(OrderStatusValue.Refunded);
    }

    [Fact]
    public void Refund_FromDelivered_TransitionsToRefunded()
    {
        var sut = new OrderBuilder().Build();
        sut.MarkAsPaid(PaymentTransactionId.NewId());
        sut.StartProcessing();
        sut.MarkAsShipped();
        sut.MarkAsDelivered();

        sut.Refund();

        sut.Status.ShouldBe(OrderStatusValue.Refunded);
    }

    [Fact]
    public void MarkAsReturned_FromShipped_TransitionsToReturned()
    {
        var sut = new OrderBuilder().Build();
        sut.MarkAsPaid(PaymentTransactionId.NewId());
        sut.StartProcessing();
        sut.MarkAsShipped();

        sut.MarkAsReturned();

        sut.Status.ShouldBe(OrderStatusValue.Returned);
    }

    [Fact]
    public void MarkAsDeleted_SetsIsDeletedTrueWithoutRaisingEventOrBumpingVersion()
    {
        var sut = new OrderBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.MarkAsDeleted();

        sut.IsDeleted.ShouldBeTrue();
        sut.UpdatedAt.ShouldNotBeNull();
        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void CanBeCancelled_DelegatesToStatusPolicy()
    {
        var sut = new OrderBuilder().Build();

        sut.CanBeCancelled().ShouldBeTrue();
        sut.MarkAsPaid(PaymentTransactionId.NewId());
        sut.StartProcessing();
        sut.MarkAsShipped();
        sut.CanBeCancelled().ShouldBeFalse();
    }

    [Fact]
    public void CanBeModified_DelegatesToStatusPolicy()
    {
        var sut = new OrderBuilder().Build();

        sut.CanBeModified().ShouldBeTrue();
        sut.MarkAsPaid(PaymentTransactionId.NewId());
        sut.CanBeModified().ShouldBeFalse();
    }

    [Fact]
    public void OrderItem_TotalPriceEqualsUnitPriceMultipliedByQuantity()
    {
        var item = new OrderItemSnapshotBuilder().WithUnitPrice(75m, "IRT").WithQuantity(4).Build();
        var sut = new OrderBuilder().WithItemSnapshots(item).Build();

        sut.OrderItems.Single().TotalPrice.Amount.ShouldBe(300m);
    }

    [Fact]
    public void LifecycleSequence_HappyPath_TransitionsThroughAllStatuses()
    {
        var sut = new OrderBuilder().Build();

        sut.MoveToPending();
        sut.MarkAsPaid(PaymentTransactionId.NewId());
        sut.StartProcessing();
        sut.MarkAsShipped();
        sut.MarkAsDelivered();

        sut.Status.ShouldBe(OrderStatusValue.Delivered);
        sut.IsPaid.ShouldBeTrue();
        sut.IsDelivered.ShouldBeTrue();
        sut.DomainEvents.OfType<OrderStatusChangedEvent>().Count().ShouldBe(5);
        sut.DomainEvents.OfType<OrderPaidEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void LifecycleSequence_VersionGrowsByEventCount()
    {
        var sut = new OrderBuilder().Build();

        sut.Version.ShouldBe(1);
        sut.MoveToPending();
        sut.Version.ShouldBe(2);
        sut.MarkAsPaid(PaymentTransactionId.NewId());
        sut.Version.ShouldBe(4);
        sut.StartProcessing();
        sut.Version.ShouldBe(5);
        sut.MarkAsShipped();
        sut.Version.ShouldBe(6);
        sut.MarkAsDelivered();
        sut.Version.ShouldBe(7);
    }

    [Fact]
    public void Equality_TwoOrdersWithSameId_AreConsideredEqualByEntitySemantics()
    {
        var sut = new OrderBuilder().Build();

        sut.Equals(sut).ShouldBeTrue();
    }

    [Fact]
    public void Equality_TwoOrdersWithDifferentIds_AreConsideredUnequal()
    {
        var a = new OrderBuilder().Build();
        var b = new OrderBuilder().Build();

        a.Equals(b).ShouldBeFalse();
    }
}
