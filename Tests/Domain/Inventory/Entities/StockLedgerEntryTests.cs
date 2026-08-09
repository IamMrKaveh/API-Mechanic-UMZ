using Domain.Inventory.Entities;
using Domain.Inventory.Enums;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Inventory.Entities;

public class StockLedgerEntryTests
{
    [Fact]
    public void StockIn_WithPositiveQuantity_ReturnsEntryWithStockInEventType()
    {
        var variantId = VariantId.NewId();

        var sut = StockLedgerEntry.StockIn(variantId, 10, 10, 0m);

        sut.EventType.ShouldBe(StockEventType.StockIn);
        sut.VariantId.ShouldBe(variantId);
        sut.QuantityDelta.ShouldBe(10);
        sut.BalanceAfter.ShouldBe(10);
        sut.UnitCost.ShouldBe(0m);
    }

    [Fact]
    public void StockIn_AssignsGeneratedNonEmptyId()
    {
        var sut = StockLedgerEntry.StockIn(VariantId.NewId(), 1, 1, 0m);

        sut.Id.ShouldNotBeNull();
        sut.Id.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void StockIn_SetsCreatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = StockLedgerEntry.StockIn(VariantId.NewId(), 1, 1, 0m);

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void StockIn_WithReferenceNumber_UsesReferenceInIdempotencyKey()
    {
        var variantId = VariantId.NewId();

        var sut = StockLedgerEntry.StockIn(variantId, 1, 1, 0m, referenceNumber: "REF-42");

        sut.IdempotencyKey.ShouldBe($"{variantId}:{StockEventType.StockIn}:REF-42");
    }

    [Fact]
    public void StockIn_WithoutReferenceNumber_UsesRandomGuidInIdempotencyKey()
    {
        var variantId = VariantId.NewId();

        var a = StockLedgerEntry.StockIn(variantId, 1, 1, 0m);
        var b = StockLedgerEntry.StockIn(variantId, 1, 1, 0m);

        a.IdempotencyKey.ShouldNotBe(b.IdempotencyKey);
        a.IdempotencyKey.ShouldStartWith($"{variantId}:{StockEventType.StockIn}:");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void StockIn_WithNonPositiveQuantity_ThrowsArgumentOutOfRangeException(int quantity)
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => StockLedgerEntry.StockIn(VariantId.NewId(), quantity, 0, 0m));
    }

    [Fact]
    public void StockIn_WithNegativeBalanceAfter_ThrowsDomainException()
    {
        var ex = Should.Throw<DomainException>(
            () => StockLedgerEntry.StockIn(VariantId.NewId(), 1, -1, 0m));

        ex.Message.ShouldBe("موجودی پس از این رویداد نمی‌تواند منفی باشد.");
    }

    [Fact]
    public void Reserve_WithPositiveQuantity_ReturnsEntryWithNegativeDeltaAndReservationEventType()
    {
        var variantId = VariantId.NewId();
        var orderItemId = OrderItemId.NewId();

        var sut = StockLedgerEntry.Reserve(variantId, 5, 5, "REF", correlationId: "CORR", orderItemId: orderItemId);

        sut.EventType.ShouldBe(StockEventType.Reservation);
        sut.QuantityDelta.ShouldBe(-5);
        sut.CorrelationId.ShouldBe("CORR");
        sut.OrderItemId.ShouldBe(orderItemId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Reserve_WithNonPositiveQuantity_ThrowsArgumentOutOfRangeException(int quantity)
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => StockLedgerEntry.Reserve(VariantId.NewId(), quantity, 0, "REF"));
    }

    [Fact]
    public void ReleaseReservation_WithPositiveQuantity_ReturnsEntryWithPositiveDelta()
    {
        var sut = StockLedgerEntry.ReleaseReservation(VariantId.NewId(), 3, 3, "REF", "reason");

        sut.EventType.ShouldBe(StockEventType.ReservationRelease);
        sut.QuantityDelta.ShouldBe(3);
        sut.Note.ShouldBe("reason");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void ReleaseReservation_WithNonPositiveQuantity_ThrowsArgumentOutOfRangeException(int quantity)
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => StockLedgerEntry.ReleaseReservation(VariantId.NewId(), quantity, 0, "REF"));
    }

    [Fact]
    public void CommitReservation_WithPositiveQuantity_ReturnsEntryWithNegativeDelta()
    {
        var orderItemId = OrderItemId.NewId();

        var sut = StockLedgerEntry.CommitReservation(VariantId.NewId(), 4, 0, "REF", orderItemId);

        sut.EventType.ShouldBe(StockEventType.ReservationCommit);
        sut.QuantityDelta.ShouldBe(-4);
        sut.OrderItemId.ShouldBe(orderItemId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CommitReservation_WithNonPositiveQuantity_ThrowsArgumentOutOfRangeException(int quantity)
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => StockLedgerEntry.CommitReservation(VariantId.NewId(), quantity, 0, "REF"));
    }

    [Fact]
    public void Adjustment_WithPositiveDelta_ReturnsEntryWithAdjustmentEventTypeAndNoteAsReason()
    {
        var userId = UserId.NewId();

        var sut = StockLedgerEntry.Adjustment(VariantId.NewId(), 5, 15, "manual correction", userId);

        sut.EventType.ShouldBe(StockEventType.Adjustment);
        sut.QuantityDelta.ShouldBe(5);
        sut.BalanceAfter.ShouldBe(15);
        sut.Note.ShouldBe("manual correction");
        sut.UserId.ShouldBe(userId);
    }

    [Fact]
    public void Adjustment_WithNegativeDelta_AllowedAndPersistsSignedDelta()
    {
        var sut = StockLedgerEntry.Adjustment(VariantId.NewId(), -3, 7, "damage");

        sut.QuantityDelta.ShouldBe(-3);
    }

    [Fact]
    public void Adjustment_WithNegativeBalanceAfter_ThrowsDomainException()
    {
        var ex = Should.Throw<DomainException>(
            () => StockLedgerEntry.Adjustment(VariantId.NewId(), -1, -1, "x"));

        ex.Message.ShouldBe("موجودی پس از این رویداد نمی‌تواند منفی باشد.");
    }

    [Fact]
    public void EventTypeName_ReturnsEnumMemberName()
    {
        var sut = StockLedgerEntry.StockIn(VariantId.NewId(), 1, 1, 0m);

        sut.EventTypeName.ShouldBe("StockIn");
    }
}
