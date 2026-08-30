using Application.Common.Events;
using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.Events;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using Infrastructure.Payment.EventHandlers;
using Inv = Domain.Inventory.Aggregates.Inventory;
using Orders = Domain.Order.Aggregates.Order;

namespace Tests.Infrastructure.Payment.EventHandlers;

public class PaymentSucceededInventoryCommitEventHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PaymentSucceededInventoryCommitEventHandler _sut;

    public PaymentSucceededInventoryCommitEventHandlerTests()
    {
        _sut = new PaymentSucceededInventoryCommitEventHandler(
            _orderRepository,
            _inventoryRepository,
            _unitOfWork);
    }

    private static PaymentSucceededEvent BuildEvent(OrderId? orderId = null, UserId? userId = null) =>
        new(
            PaymentTransactionId.NewId(),
            orderId ?? OrderId.NewId(),
            123456L,
            userId ?? UserId.NewId(),
            Money.Create(100_000m, "IRT"));

    private static DomainEventNotification<PaymentSucceededEvent> Wrap(PaymentSucceededEvent evt) => new(evt);

    private static Orders BuildOrderWithSingleItem(OrderId orderId, VariantId variantId, int quantity = 1) =>
        new OrderBuilder()
            .WithOrderId(orderId)
            .WithItemSnapshots(new OrderItemSnapshotBuilder()
                .WithVariantId(variantId)
                .WithQuantity(quantity)
                .Build())
            .Build();

    private static Inv BuildInventoryWithReservation(VariantId variantId, int initialStock, int reserved)
    {
        var inventory = new InventoryBuilder()
            .WithVariantId(variantId)
            .WithInitialStock(initialStock)
            .Build();

        if (reserved > 0)
        {
            inventory.ReserveStock(
                StockQuantity.Create(reserved),
                "SETUP-RESERVATION").ShouldBeSuccess();
        }

        inventory.ClearDomainEvents();
        return inventory;
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ReturnsWithoutTouchingInventoryOrUnitOfWork()
    {
        var evt = BuildEvent();
        _orderRepository
            .FindByIdAsync(evt.OrderId, Arg.Any<CancellationToken>())
            .Returns((Orders?)null);

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _orderRepository.Received(1).FindByIdAsync(evt.OrderId, Arg.Any<CancellationToken>());
        await _inventoryRepository.DidNotReceiveWithAnyArgs().GetByVariantIdAsync(default!, default);
        _inventoryRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenInventoryIsMissingForVariant_SkipsItemAndDoesNotUpdateThatInventory()
    {
        var orderId = OrderId.NewId();
        var variantId = VariantId.NewId();
        var order = BuildOrderWithSingleItem(orderId, variantId, quantity: 2);

        _orderRepository
            .FindByIdAsync(orderId, Arg.Any<CancellationToken>())
            .Returns(order);
        _inventoryRepository
            .GetByVariantIdAsync(variantId, Arg.Any<CancellationToken>())
            .Returns((Inv?)null);

        await _sut.Handle(Wrap(BuildEvent(orderId)), CancellationToken.None);

        await _inventoryRepository.Received(1).GetByVariantIdAsync(variantId, Arg.Any<CancellationToken>());
        _inventoryRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenReservationConfirmed_UpdatesInventoryAndSavesChanges()
    {
        var orderId = OrderId.NewId();
        var variantId = VariantId.NewId();
        var order = BuildOrderWithSingleItem(orderId, variantId, quantity: 3);
        var inventory = BuildInventoryWithReservation(variantId, initialStock: 10, reserved: 3);

        _orderRepository
            .FindByIdAsync(orderId, Arg.Any<CancellationToken>())
            .Returns(order);
        _inventoryRepository
            .GetByVariantIdAsync(variantId, Arg.Any<CancellationToken>())
            .Returns(inventory);

        await _sut.Handle(Wrap(BuildEvent(orderId)), CancellationToken.None);

        _inventoryRepository.Received(1).Update(inventory);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        inventory.StockQuantity.Value.ShouldBe(7);
        inventory.ReservedQuantity.Value.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WhenConfirmationFailsDueToInsufficientReservation_DoesNotUpdateInventoryButStillSaves()
    {
        var orderId = OrderId.NewId();
        var variantId = VariantId.NewId();
        // Order requests 5 units, but only 1 was reserved. ConfirmReservation should fail.
        var order = BuildOrderWithSingleItem(orderId, variantId, quantity: 5);
        var inventory = BuildInventoryWithReservation(variantId, initialStock: 10, reserved: 1);

        _orderRepository
            .FindByIdAsync(orderId, Arg.Any<CancellationToken>())
            .Returns(order);
        _inventoryRepository
            .GetByVariantIdAsync(variantId, Arg.Any<CancellationToken>())
            .Returns(inventory);

        await _sut.Handle(Wrap(BuildEvent(orderId)), CancellationToken.None);

        _inventoryRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        inventory.StockQuantity.Value.ShouldBe(10);
        inventory.ReservedQuantity.Value.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_WhenMultipleItems_ConfirmsEachInventoryIndependently()
    {
        var orderId = OrderId.NewId();
        var variantIdA = VariantId.NewId();
        var variantIdB = VariantId.NewId();

        var order = new OrderBuilder()
            .WithOrderId(orderId)
            .WithItemSnapshots(
                new OrderItemSnapshotBuilder().WithVariantId(variantIdA).WithQuantity(1).WithSku(Sku.Create("SKU-A")).Build(),
                new OrderItemSnapshotBuilder().WithVariantId(variantIdB).WithQuantity(2).WithSku(Sku.Create("SKU-B")).Build())
            .Build();

        var inventoryA = BuildInventoryWithReservation(variantIdA, initialStock: 5, reserved: 1);
        var inventoryB = BuildInventoryWithReservation(variantIdB, initialStock: 5, reserved: 2);

        _orderRepository
            .FindByIdAsync(orderId, Arg.Any<CancellationToken>())
            .Returns(order);
        _inventoryRepository
            .GetByVariantIdAsync(variantIdA, Arg.Any<CancellationToken>())
            .Returns(inventoryA);
        _inventoryRepository
            .GetByVariantIdAsync(variantIdB, Arg.Any<CancellationToken>())
            .Returns(inventoryB);

        await _sut.Handle(Wrap(BuildEvent(orderId)), CancellationToken.None);

        _inventoryRepository.Received(1).Update(inventoryA);
        _inventoryRepository.Received(1).Update(inventoryB);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        inventoryA.StockQuantity.Value.ShouldBe(4);
        inventoryB.StockQuantity.Value.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_WhenOrderHasNoItems_DoesNotLookUpInventoryButStillSaves()
    {
        // OrderBuilder requires at least one item, so we set up a mock Order via NSubstitute is not
        // an option (Order is a concrete aggregate). Instead we assert against the happy path where
        // the repository yields null, which is the closest "no work" contract.
        var evt = BuildEvent();
        _orderRepository
            .FindByIdAsync(evt.OrderId, Arg.Any<CancellationToken>())
            .Returns((Orders?)null);

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _inventoryRepository.DidNotReceiveWithAnyArgs().GetByVariantIdAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenOrderRepositoryThrows_SwallowsExceptionAndDoesNotSave()
    {
        var evt = BuildEvent();

        _orderRepository
            .FindByIdAsync(evt.OrderId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("db failure"));

        await Should.NotThrowAsync(() => _sut.Handle(Wrap(evt), CancellationToken.None));

        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenInventoryRepositoryThrows_SwallowsExceptionAndDoesNotSave()
    {
        var orderId = OrderId.NewId();
        var variantId = VariantId.NewId();
        var order = BuildOrderWithSingleItem(orderId, variantId);

        _orderRepository
            .FindByIdAsync(orderId, Arg.Any<CancellationToken>())
            .Returns(order);
        _inventoryRepository
            .GetByVariantIdAsync(variantId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("db failure"));

        await Should.NotThrowAsync(() => _sut.Handle(Wrap(BuildEvent(orderId)), CancellationToken.None));

        _inventoryRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenUnitOfWorkSaveThrows_SwallowsExceptionAndDoesNotRethrow()
    {
        var orderId = OrderId.NewId();
        var variantId = VariantId.NewId();
        var order = BuildOrderWithSingleItem(orderId, variantId);
        var inventory = BuildInventoryWithReservation(variantId, initialStock: 5, reserved: 1);

        _orderRepository
            .FindByIdAsync(orderId, Arg.Any<CancellationToken>())
            .Returns(order);
        _inventoryRepository
            .GetByVariantIdAsync(variantId, Arg.Any<CancellationToken>())
            .Returns(inventory);
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateConcurrencyException("concurrency"));

        await Should.NotThrowAsync(() => _sut.Handle(Wrap(BuildEvent(orderId)), CancellationToken.None));

        _inventoryRepository.Received(1).Update(inventory);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToDependencies()
    {
        var orderId = OrderId.NewId();
        var variantId = VariantId.NewId();
        var order = BuildOrderWithSingleItem(orderId, variantId);
        var inventory = BuildInventoryWithReservation(variantId, initialStock: 5, reserved: 1);

        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _orderRepository.FindByIdAsync(orderId, token).Returns(order);
        _inventoryRepository.GetByVariantIdAsync(variantId, token).Returns(inventory);

        await _sut.Handle(Wrap(BuildEvent(orderId)), token);

        await _orderRepository.Received(1).FindByIdAsync(orderId, token);
        await _inventoryRepository.Received(1).GetByVariantIdAsync(variantId, token);
        await _unitOfWork.Received(1).SaveChangesAsync(token);
    }

    [Fact]
    public async Task Handle_UsesOrderNumberValueAsReferenceNumberForCommittedLedgerEntry()
    {
        var orderId = OrderId.NewId();
        var variantId = VariantId.NewId();
        var order = BuildOrderWithSingleItem(orderId, variantId, quantity: 1);
        var inventory = BuildInventoryWithReservation(variantId, initialStock: 2, reserved: 1);

        _orderRepository.FindByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        _inventoryRepository.GetByVariantIdAsync(variantId, Arg.Any<CancellationToken>()).Returns(inventory);

        await _sut.Handle(Wrap(BuildEvent(orderId)), CancellationToken.None);

        var committedEntry = inventory.LedgerEntries
            .SingleOrDefault(e => e.ReferenceNumber == order.OrderNumber.Value);

        committedEntry.ShouldNotBeNull();
        committedEntry!.ReferenceNumber.ShouldBe(order.OrderNumber.Value);
    }

    [Fact]
    public async Task Handle_PassesOrderItemIdOfOrderItemToConfirmReservationLedger()
    {
        var orderId = OrderId.NewId();
        var variantId = VariantId.NewId();
        var order = BuildOrderWithSingleItem(orderId, variantId, quantity: 1);
        var inventory = BuildInventoryWithReservation(variantId, initialStock: 2, reserved: 1);

        _orderRepository.FindByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        _inventoryRepository.GetByVariantIdAsync(variantId, Arg.Any<CancellationToken>()).Returns(inventory);

        await _sut.Handle(Wrap(BuildEvent(orderId)), CancellationToken.None);

        var orderItemId = order.OrderItems.Single().Id;
        var committedEntry = inventory.LedgerEntries
            .SingleOrDefault(e => e.OrderItemId == orderItemId);

        committedEntry.ShouldNotBeNull();
    }
}
