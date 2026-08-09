using Domain.Inventory.Enums;
using Domain.Inventory.Events;
using Domain.Inventory.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Inv = Domain.Inventory.Aggregates.Inventory;

namespace Tests.Domain.Inventory.Aggregates;

public class InventoryTests
{
    [Fact]
    public void Create_WithVariantIdAndDefaults_ReturnsInitializedInventory()
    {
        var variantId = VariantId.NewId();

        var sut = new InventoryBuilder().WithVariantId(variantId).Build();

        sut.ShouldNotBeNull();
        sut.Id.ShouldNotBeNull();
        sut.Id.Value.ShouldNotBe(Guid.Empty);
        sut.VariantId.ShouldBe(variantId);
        sut.StockQuantity.Value.ShouldBe(0);
        sut.ReservedQuantity.Value.ShouldBe(0);
        sut.IsUnlimited.ShouldBeFalse();
        sut.LowStockThreshold.ShouldBe(5);
        sut.LedgerEntries.ShouldBeEmpty();
    }

    [Fact]
    public void Create_SetsCreatedAtAndUpdatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new InventoryBuilder().Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
        sut.UpdatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.UpdatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Create_ImplementsISoftDeletableWithDefaults()
    {
        var sut = new InventoryBuilder().Build();

        sut.ShouldBeAssignableTo<ISoftDeletable>();
        sut.IsDeleted.ShouldBeFalse();
        sut.DeletedAt.ShouldBeNull();
        sut.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public void Create_WithPositiveInitialStock_AddsSingleStockInLedgerEntry()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();

        sut.StockQuantity.Value.ShouldBe(10);
        sut.LedgerEntries.Count.ShouldBe(1);
        var entry = sut.LedgerEntries.Single();
        entry.EventType.ShouldBe(StockEventType.StockIn);
        entry.QuantityDelta.ShouldBe(10);
        entry.BalanceAfter.ShouldBe(10);
        entry.Note.ShouldBe("ایجاد موجودی اولیه برای واریانت");
    }

    [Fact]
    public void Create_WithPositiveInitialStockAndUnlimitedTrue_DoesNotAddLedgerEntry()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).AsUnlimited().Build();

        sut.LedgerEntries.ShouldBeEmpty();
    }

    [Fact]
    public void Create_WithZeroInitialStock_DoesNotAddLedgerEntry()
    {
        var sut = new InventoryBuilder().WithInitialStock(0).Build();

        sut.LedgerEntries.ShouldBeEmpty();
    }

    [Fact]
    public void Create_RaisesExactlyOneInventoryCreatedEvent()
    {
        var variantId = VariantId.NewId();

        var sut = new InventoryBuilder()
            .WithVariantId(variantId)
            .WithInitialStock(7)
            .Build();

        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.OfType<InventoryCreatedEvent>().Single();
        evt.InventoryId.ShouldBe(sut.Id);
        evt.VariantId.ShouldBe(variantId);
        evt.InitialStock.ShouldBe(7);
        evt.IsUnlimited.ShouldBeFalse();
    }

    [Fact]
    public void Create_WithNegativeInitialStock_ThrowsDomainException()
    {
        var ex = Should.Throw<DomainException>(
            () => new InventoryBuilder().WithInitialStock(-1).Build());

        ex.Message.ShouldBe("موجودی اولیه نمی‌تواند منفی باشد.");
    }

    [Fact]
    public void Create_WithNegativeLowStockThreshold_ThrowsDomainException()
    {
        var ex = Should.Throw<DomainException>(
            () => new InventoryBuilder().WithLowStockThreshold(-1).Build());

        ex.Message.ShouldBe("آستانه کمبود موجودی نمی‌تواند منفی باشد.");
    }

    [Fact]
    public void Create_WithNullVariantId_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(
            () => Inv.Create(null!, 0, false, 5, null));
    }

    [Fact]
    public void AvailableQuantity_WhenNotUnlimited_ReturnsStockMinusReserved()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        sut.ReserveStock(StockQuantity.Create(3), "REF").ShouldBeSuccess();

        sut.AvailableQuantity.ShouldBe(7);
    }

    [Fact]
    public void AvailableQuantity_WhenUnlimited_ReturnsIntMaxValue()
    {
        var sut = new InventoryBuilder().AsUnlimited().Build();

        sut.AvailableQuantity.ShouldBe(int.MaxValue);
    }

    [Fact]
    public void IsInStock_WhenAvailableQuantityPositive_ReturnsTrue()
    {
        var sut = new InventoryBuilder().WithInitialStock(5).Build();

        sut.IsInStock.ShouldBeTrue();
    }

    [Fact]
    public void IsInStock_WhenAvailableQuantityZero_ReturnsFalse()
    {
        var sut = new InventoryBuilder().Build();

        sut.IsInStock.ShouldBeFalse();
        sut.IsOutOfStock.ShouldBeTrue();
    }

    [Fact]
    public void IsInStock_WhenUnlimited_ReturnsTrue()
    {
        var sut = new InventoryBuilder().AsUnlimited().Build();

        sut.IsInStock.ShouldBeTrue();
        sut.IsOutOfStock.ShouldBeFalse();
    }

    [Fact]
    public void IsLowStock_WhenAvailableAtOrBelowThresholdAndAboveZero_ReturnsTrue()
    {
        var sut = new InventoryBuilder().WithInitialStock(3).WithLowStockThreshold(5).Build();

        sut.IsLowStock.ShouldBeTrue();
    }

    [Fact]
    public void IsLowStock_WhenAvailableAboveThreshold_ReturnsFalse()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).WithLowStockThreshold(5).Build();

        sut.IsLowStock.ShouldBeFalse();
    }

    [Fact]
    public void IsLowStock_WhenAvailableIsZero_ReturnsFalse()
    {
        var sut = new InventoryBuilder().Build();

        sut.IsLowStock.ShouldBeFalse();
    }

    [Fact]
    public void IsLowStock_WhenUnlimited_ReturnsFalse()
    {
        var sut = new InventoryBuilder().AsUnlimited().Build();

        sut.IsLowStock.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void IncreaseStock_WithNonPositiveQuantity_ReturnsInvalidQuantityFailure(int quantity)
    {
        var sut = new InventoryBuilder().Build();

        var result = sut.IncreaseStock(quantity, "reason");

        result.ShouldFailWith("Inventory.InvalidQuantity");
    }

    [Fact]
    public void IncreaseStock_WithPositiveQuantity_IncreasesStockAndAppendsStockInLedgerEntry()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        sut.ClearDomainEvents();

        var result = sut.IncreaseStock(5, "restock");

        result.ShouldBeSuccess();
        sut.StockQuantity.Value.ShouldBe(15);
        sut.LedgerEntries.Count.ShouldBe(2);
        var last = sut.LedgerEntries.Last();
        last.EventType.ShouldBe(StockEventType.StockIn);
        last.QuantityDelta.ShouldBe(5);
        last.Note.ShouldBe("restock");
    }

    [Fact]
    public void IncreaseStock_OnUnlimitedInventory_TurnsOffIsUnlimited()
    {
        var sut = new InventoryBuilder().AsUnlimited().Build();

        sut.IncreaseStock(5, "restock").ShouldBeSuccess();

        sut.IsUnlimited.ShouldBeFalse();
        sut.StockQuantity.Value.ShouldBe(5);
    }

    [Fact]
    public void IncreaseStock_RaisesStockIncreasedEventWithNewQuantityAndReason()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        sut.ClearDomainEvents();

        sut.IncreaseStock(5, "restock").ShouldBeSuccess();

        var evt = sut.DomainEvents.OfType<StockIncreasedEvent>().Single();
        evt.InventoryId.ShouldBe(sut.Id);
        evt.VariantId.ShouldBe(sut.VariantId);
        evt.QuantityAdded.ShouldBe(5);
        evt.NewStockQuantity.ShouldBe(15);
        evt.Reason.ShouldBe("restock");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DecreaseStock_WithNonPositiveQuantity_ReturnsInvalidQuantityFailure(int quantity)
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();

        sut.DecreaseStock(quantity, "reason").ShouldFailWith("Inventory.InvalidQuantity");
    }

    [Fact]
    public void DecreaseStock_WithSufficientStock_ReducesStockAndAppendsAdjustmentLedgerEntry()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();

        sut.DecreaseStock(3, "sold").ShouldBeSuccess();

        sut.StockQuantity.Value.ShouldBe(7);
        var last = sut.LedgerEntries.Last();
        last.EventType.ShouldBe(StockEventType.Adjustment);
        last.QuantityDelta.ShouldBe(-3);
        last.Note.ShouldBe("sold");
    }

    [Fact]
    public void DecreaseStock_WithInsufficientStock_ReturnsValidationFailureAndLeavesStockUnchanged()
    {
        var sut = new InventoryBuilder().WithInitialStock(5).Build();

        var result = sut.DecreaseStock(10, "sold");

        result.ShouldFailWith("400");
        result.ShouldFailWithType(ErrorType.Validation);
        sut.StockQuantity.Value.ShouldBe(5);
    }

    [Fact]
    public void DecreaseStock_WhenUnlimited_ReturnsSuccessAndLeavesStockQuantityUnchanged()
    {
        var sut = new InventoryBuilder().AsUnlimited().Build();
        var stockBefore = sut.StockQuantity.Value;

        sut.DecreaseStock(5, "sold").ShouldBeSuccess();

        sut.StockQuantity.Value.ShouldBe(stockBefore);
    }

    [Fact]
    public void DecreaseStock_RaisesStockDecreasedEvent()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        sut.ClearDomainEvents();

        sut.DecreaseStock(3, "sold").ShouldBeSuccess();

        var evt = sut.DomainEvents.OfType<StockDecreasedEvent>().Single();
        evt.QuantityRemoved.ShouldBe(3);
        evt.NewStockQuantity.ShouldBe(7);
        evt.Reason.ShouldBe("sold");
    }

    [Fact]
    public void SetUnlimited_SetsIsUnlimitedTrueAndRaisesStockSetUnlimitedEvent()
    {
        var sut = new InventoryBuilder().Build();
        sut.ClearDomainEvents();

        sut.SetUnlimited();

        sut.IsUnlimited.ShouldBeTrue();
        sut.DomainEvents.OfType<StockSetUnlimitedEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void SetLowStockThreshold_WithNonNegativeValue_UpdatesThreshold()
    {
        var sut = new InventoryBuilder().Build();

        sut.SetLowStockThreshold(20);

        sut.LowStockThreshold.ShouldBe(20);
    }

    [Fact]
    public void SetLowStockThreshold_WithNegativeValue_ThrowsDomainException()
    {
        var sut = new InventoryBuilder().Build();

        var ex = Should.Throw<DomainException>(() => sut.SetLowStockThreshold(-1));

        ex.Message.ShouldBe("آستانه کمبود موجودی نمی‌تواند منفی باشد.");
    }

    [Fact]
    public void ReserveStock_WithZeroQuantity_ReturnsInvalidQuantityFailure()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();

        sut.ReserveStock(StockQuantity.Create(0), "REF").ShouldFailWith("Inventory.InvalidQuantity");
    }

    [Fact]
    public void ReserveStock_WithSufficientAvailable_IncreasesReservedAndAppendsReserveLedgerEntry()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();

        sut.ReserveStock(StockQuantity.Create(4), "REF").ShouldBeSuccess();

        sut.ReservedQuantity.Value.ShouldBe(4);
        sut.AvailableQuantity.ShouldBe(6);
        sut.StockQuantity.Value.ShouldBe(10);
        var last = sut.LedgerEntries.Last();
        last.EventType.ShouldBe(StockEventType.Reservation);
        last.QuantityDelta.ShouldBe(-4);
    }

    [Fact]
    public void ReserveStock_WithInsufficientAvailable_ReturnsValidationFailureAndLeavesReservedUnchanged()
    {
        var sut = new InventoryBuilder().WithInitialStock(3).Build();

        var result = sut.ReserveStock(StockQuantity.Create(10), "REF");

        result.ShouldFailWith("400");
        sut.ReservedQuantity.Value.ShouldBe(0);
    }

    [Fact]
    public void ReserveStock_WhenUnlimited_SucceedsAndDoesNotChangeReservedQuantity()
    {
        var sut = new InventoryBuilder().AsUnlimited().Build();

        sut.ReserveStock(StockQuantity.Create(5), "REF").ShouldBeSuccess();

        sut.ReservedQuantity.Value.ShouldBe(0);
    }

    [Fact]
    public void ReserveStock_RaisesStockReservedEventWithTotalReserved()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        sut.ClearDomainEvents();

        sut.ReserveStock(StockQuantity.Create(4), "REF").ShouldBeSuccess();

        var evt = sut.DomainEvents.OfType<StockReservedEvent>().Single();
        evt.QuantityReserved.ShouldBe(4);
        evt.TotalReservedQuantity.ShouldBe(4);
    }

    [Fact]
    public void ReleaseReservation_WithZeroQuantity_ReturnsInvalidQuantityFailure()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();

        sut.ReleaseReservation(StockQuantity.Create(0), "REF").ShouldFailWith("Inventory.InvalidQuantity");
    }

    [Fact]
    public void ReleaseReservation_WhenUnlimited_ReturnsSuccessWithoutSideEffects()
    {
        var sut = new InventoryBuilder().AsUnlimited().Build();
        sut.ClearDomainEvents();

        sut.ReleaseReservation(StockQuantity.Create(5), "REF").ShouldBeSuccess();

        sut.DomainEvents.ShouldBeEmpty();
        sut.LedgerEntries.ShouldBeEmpty();
    }

    [Fact]
    public void ReleaseReservation_WithNothingReserved_ReturnsSuccessAsNoOp()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        sut.ClearDomainEvents();

        sut.ReleaseReservation(StockQuantity.Create(5), "REF").ShouldBeSuccess();

        sut.ReservedQuantity.Value.ShouldBe(0);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ReleaseReservation_WithMoreThanReserved_ClampsToReservedAmount()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        sut.ReserveStock(StockQuantity.Create(3), "REF").ShouldBeSuccess();

        sut.ReleaseReservation(StockQuantity.Create(100), "REF").ShouldBeSuccess();

        sut.ReservedQuantity.Value.ShouldBe(0);
        sut.AvailableQuantity.ShouldBe(10);
    }

    [Fact]
    public void ReleaseReservation_WithPartialAmount_ReducesReservedByExactAmount()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        sut.ReserveStock(StockQuantity.Create(5), "REF").ShouldBeSuccess();
        sut.ClearDomainEvents();

        sut.ReleaseReservation(StockQuantity.Create(2), "REF").ShouldBeSuccess();

        sut.ReservedQuantity.Value.ShouldBe(3);
        sut.DomainEvents.OfType<StockReservationReleasedEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void ConfirmReservation_WithZeroQuantity_ReturnsInvalidQuantityFailure()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();

        sut.ConfirmReservation(StockQuantity.Create(0), "REF").ShouldFailWith("Inventory.InvalidQuantity");
    }

    [Fact]
    public void ConfirmReservation_WhenUnlimited_ReturnsSuccessWithoutSideEffects()
    {
        var sut = new InventoryBuilder().AsUnlimited().Build();
        sut.ClearDomainEvents();

        sut.ConfirmReservation(StockQuantity.Create(3), "REF").ShouldBeSuccess();

        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ConfirmReservation_WithGreaterThanReserved_ReturnsInsufficientReservationFailure()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        sut.ReserveStock(StockQuantity.Create(2), "REF").ShouldBeSuccess();

        sut.ConfirmReservation(StockQuantity.Create(5), "REF")
            .ShouldFailWith("Inventory.InsufficientReservation");
    }

    [Fact]
    public void ConfirmReservation_WithReservedAmount_ReducesReservedAndStockAndAppendsCommitLedger()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        sut.ReserveStock(StockQuantity.Create(4), "REF").ShouldBeSuccess();

        sut.ConfirmReservation(StockQuantity.Create(4), "REF").ShouldBeSuccess();

        sut.ReservedQuantity.Value.ShouldBe(0);
        sut.StockQuantity.Value.ShouldBe(6);
        sut.LedgerEntries.Last().EventType.ShouldBe(StockEventType.ReservationCommit);
    }

    [Fact]
    public void ConfirmReservation_RaisesStockCommittedEvent()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        sut.ReserveStock(StockQuantity.Create(4), "REF").ShouldBeSuccess();
        sut.ClearDomainEvents();

        sut.ConfirmReservation(StockQuantity.Create(4), "REF").ShouldBeSuccess();

        var evt = sut.DomainEvents.OfType<StockCommittedEvent>().Single();
        evt.InventoryId.ShouldBe(sut.Id);
        evt.VariantId.ShouldBe(sut.VariantId);
        evt.Quantity.ShouldBe(4);
    }

    [Fact]
    public void ReverseStockChange_WhenUnlimited_ReturnsNotApplicableFailure()
    {
        var sut = new InventoryBuilder().AsUnlimited().Build();

        sut.ReverseStockChange("nonexistent-key", "reason", UserId.NewId())
            .ShouldFailWith("Inventory.NotApplicable");
    }

    [Fact]
    public void ReverseStockChange_WithUnknownIdempotencyKey_ReturnsNotFoundFailure()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();

        sut.ReverseStockChange("no-such-key", "reason", UserId.NewId())
            .ShouldFailWith("Inventory.NotFound");
    }

    [Fact]
    public void ReverseStockChange_WithKnownStockInEntry_SubtractsFromCurrentStock()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        var originalKey = sut.LedgerEntries.Single().IdempotencyKey;

        var result = sut.ReverseStockChange(originalKey, "return", UserId.NewId());

        result.ShouldBeSuccess();
        sut.StockQuantity.Value.ShouldBe(0);
        sut.LedgerEntries.Last().EventType.ShouldBe(StockEventType.Adjustment);
    }

    [Fact]
    public void ReverseStockChange_WhenReversalWouldGoNegative_ReturnsFailureAndLeavesStockUnchanged()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        var originalKey = sut.LedgerEntries.Single().IdempotencyKey;
        sut.DecreaseStock(8, "sold").ShouldBeSuccess();

        var result = sut.ReverseStockChange(originalKey, "return", UserId.NewId());

        result.IsFailure.ShouldBeTrue();
        sut.StockQuantity.Value.ShouldBe(2);
    }

    [Fact]
    public void ReverseStockChange_OnSuccess_RaisesStockAdjustedEvent()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        var originalKey = sut.LedgerEntries.Single().IdempotencyKey;
        sut.ClearDomainEvents();

        sut.ReverseStockChange(originalKey, "return", UserId.NewId()).ShouldBeSuccess();

        sut.DomainEvents.OfType<StockAdjustedEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void ReturnStock_WithZeroQuantity_ReturnsInvalidQuantityFailure()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();

        sut.ReturnStock(StockQuantity.Create(0), "return").ShouldFailWith("Inventory.InvalidQuantity");
    }

    [Fact]
    public void ReturnStock_WhenUnlimited_ReturnsSuccessWithoutStateChange()
    {
        var sut = new InventoryBuilder().AsUnlimited().Build();
        sut.ClearDomainEvents();

        sut.ReturnStock(StockQuantity.Create(3), "return").ShouldBeSuccess();

        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ReturnStock_WithPositiveQuantity_IncreasesStockAndAppendsStockInLedgerEntry()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();

        sut.ReturnStock(StockQuantity.Create(3), "customer return").ShouldBeSuccess();

        sut.StockQuantity.Value.ShouldBe(13);
        var last = sut.LedgerEntries.Last();
        last.EventType.ShouldBe(StockEventType.StockIn);
        last.Note.ShouldBe("customer return");
    }

    [Fact]
    public void ReturnStock_RaisesStockRestoredEvent()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        sut.ClearDomainEvents();

        sut.ReturnStock(StockQuantity.Create(3), "customer return").ShouldBeSuccess();

        sut.DomainEvents.OfType<StockRestoredEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void AdjustStock_WhenUnlimited_ReturnsNotApplicableFailure()
    {
        var sut = new InventoryBuilder().AsUnlimited().Build();

        sut.AdjustStock(1, UserId.NewId(), "x").ShouldFailWith("Inventory.NotApplicable");
    }

    [Fact]
    public void AdjustStock_WithPositiveChange_IncreasesStock()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();

        sut.AdjustStock(5, UserId.NewId(), "recount").ShouldBeSuccess();

        sut.StockQuantity.Value.ShouldBe(15);
    }

    [Fact]
    public void AdjustStock_WithNegativeChangeAndSufficientStock_DecreasesStock()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();

        sut.AdjustStock(-4, UserId.NewId(), "recount").ShouldBeSuccess();

        sut.StockQuantity.Value.ShouldBe(6);
    }

    [Fact]
    public void AdjustStock_WithNegativeChangeGreaterThanStock_ReturnsFailure()
    {
        var sut = new InventoryBuilder().WithInitialStock(3).Build();

        var result = sut.AdjustStock(-10, UserId.NewId(), "recount");

        result.IsFailure.ShouldBeTrue();
        sut.StockQuantity.Value.ShouldBe(3);
    }

    [Fact]
    public void AdjustStock_RaisesStockAdjustedEventWithSignedDelta()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        sut.ClearDomainEvents();

        sut.AdjustStock(-4, UserId.NewId(), "recount").ShouldBeSuccess();

        var evt = sut.DomainEvents.OfType<StockAdjustedEvent>().Single();
        evt.Adjustment.ShouldBe(-4);
        evt.NewQuantity.ShouldBe(6);
        evt.Reason.ShouldBe("recount");
    }

    [Fact]
    public void AdjustStockTo_WithNegativeTarget_ReturnsInvalidQuantityFailure()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();

        sut.AdjustStockTo(-1, "correction").ShouldFailWith("Inventory.InvalidQuantity");
    }

    [Fact]
    public void AdjustStockTo_WhenUnlimited_ReturnsSuccessWithoutStateChange()
    {
        var sut = new InventoryBuilder().AsUnlimited().Build();
        sut.ClearDomainEvents();

        sut.AdjustStockTo(5, "correction").ShouldBeSuccess();

        sut.IsUnlimited.ShouldBeTrue();
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void AdjustStockTo_WhenTargetEqualsCurrentStock_ReturnsSuccessAsNoOp()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        sut.ClearDomainEvents();

        sut.AdjustStockTo(10, "correction").ShouldBeSuccess();

        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void AdjustStockTo_WhenTargetGreater_DelegatesToIncreaseStockAndRaisesStockIncreased()
    {
        var sut = new InventoryBuilder().WithInitialStock(5).Build();
        sut.ClearDomainEvents();

        sut.AdjustStockTo(12, "correction").ShouldBeSuccess();

        sut.StockQuantity.Value.ShouldBe(12);
        sut.DomainEvents.OfType<StockIncreasedEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void AdjustStockTo_WhenTargetSmaller_DelegatesToDecreaseStockAndRaisesStockDecreased()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        sut.ClearDomainEvents();

        sut.AdjustStockTo(3, "correction").ShouldBeSuccess();

        sut.StockQuantity.Value.ShouldBe(3);
        sut.DomainEvents.OfType<StockDecreasedEvent>().Count().ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RecordDamage_WithNonPositiveQuantity_ReturnsInvalidQuantityFailure(int quantity)
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();

        sut.RecordDamage(quantity, UserId.NewId(), "damaged").ShouldFailWith("Inventory.InvalidQuantity");
    }

    [Fact]
    public void RecordDamage_WhenUnlimited_ReturnsNotApplicableFailure()
    {
        var sut = new InventoryBuilder().AsUnlimited().Build();

        sut.RecordDamage(1, UserId.NewId(), "damaged").ShouldFailWith("Inventory.NotApplicable");
    }

    [Fact]
    public void RecordDamage_WithInsufficientStock_ReturnsValidationFailure()
    {
        var sut = new InventoryBuilder().WithInitialStock(2).Build();

        var result = sut.RecordDamage(5, UserId.NewId(), "damaged");

        result.IsFailure.ShouldBeTrue();
        sut.StockQuantity.Value.ShouldBe(2);
    }

    [Fact]
    public void RecordDamage_WithSufficientStock_ReducesStockAndAppendsAdjustmentLedger()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        sut.ClearDomainEvents();

        sut.RecordDamage(3, UserId.NewId(), "broken").ShouldBeSuccess();

        sut.StockQuantity.Value.ShouldBe(7);
        var last = sut.LedgerEntries.Last();
        last.EventType.ShouldBe(StockEventType.Adjustment);
        last.QuantityDelta.ShouldBe(-3);
        last.Note.ShouldBe("ضایعات: broken");
    }

    [Fact]
    public void RecordDamage_DoesNotRaiseAnyDomainEvent()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        sut.ClearDomainEvents();

        sut.RecordDamage(3, UserId.NewId(), "broken").ShouldBeSuccess();

        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Reconcile_WhenUnlimited_ReturnsSuccessWithoutStateChange()
    {
        var sut = new InventoryBuilder().AsUnlimited().Build();
        sut.ClearDomainEvents();

        sut.Reconcile(StockQuantity.Create(42), UserId.NewId()).ShouldBeSuccess();

        sut.IsUnlimited.ShouldBeTrue();
        sut.DomainEvents.ShouldBeEmpty();
        sut.LedgerEntries.ShouldBeEmpty();
    }

    [Fact]
    public void Reconcile_WhenCalculatedEqualsCurrent_ReturnsSuccessWithoutSideEffect()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        var ledgerCountBefore = sut.LedgerEntries.Count;
        sut.ClearDomainEvents();

        sut.Reconcile(StockQuantity.Create(10), UserId.NewId()).ShouldBeSuccess();

        sut.LedgerEntries.Count.ShouldBe(ledgerCountBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Reconcile_WhenCalculatedGreater_IncreasesStockAndAppendsAdjustmentLedger()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();

        sut.Reconcile(StockQuantity.Create(15), UserId.NewId()).ShouldBeSuccess();

        sut.StockQuantity.Value.ShouldBe(15);
        sut.LedgerEntries.Last().EventType.ShouldBe(StockEventType.Adjustment);
        sut.LedgerEntries.Last().QuantityDelta.ShouldBe(5);
    }

    [Fact]
    public void Reconcile_WhenCalculatedSmaller_DecreasesStockAndAppendsAdjustmentLedger()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();

        sut.Reconcile(StockQuantity.Create(6), UserId.NewId()).ShouldBeSuccess();

        sut.StockQuantity.Value.ShouldBe(6);
        sut.LedgerEntries.Last().QuantityDelta.ShouldBe(-4);
    }

    [Fact]
    public void Reconcile_DoesNotRaiseAnyDomainEvent()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        sut.ClearDomainEvents();

        sut.Reconcile(StockQuantity.Create(15), UserId.NewId()).ShouldBeSuccess();

        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void CanFulfill_WhenUnlimited_ReturnsTrueForAnyQuantity()
    {
        var sut = new InventoryBuilder().AsUnlimited().Build();

        sut.CanFulfill(1_000_000).ShouldBeTrue();
    }

    [Fact]
    public void CanFulfill_WhenAvailableAtOrAboveRequested_ReturnsTrue()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();

        sut.CanFulfill(10).ShouldBeTrue();
        sut.CanFulfill(5).ShouldBeTrue();
    }

    [Fact]
    public void CanFulfill_WhenAvailableBelowRequested_ReturnsFalse()
    {
        var sut = new InventoryBuilder().WithInitialStock(5).Build();

        sut.CanFulfill(10).ShouldBeFalse();
    }

    [Fact]
    public void CanFulfill_ConsidersReservedQuantity()
    {
        var sut = new InventoryBuilder().WithInitialStock(10).Build();
        sut.ReserveStock(StockQuantity.Create(6), "REF").ShouldBeSuccess();

        sut.CanFulfill(5).ShouldBeFalse();
        sut.CanFulfill(4).ShouldBeTrue();
    }
}
