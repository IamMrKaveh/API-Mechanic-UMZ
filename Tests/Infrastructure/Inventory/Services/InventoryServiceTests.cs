using Domain.Inventory.Aggregates;
using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;
using Domain.Order.Interfaces;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using Infrastructure.Inventory.Services;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Infrastructure.Inventory.Services;

public class InventoryServiceTests
{
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>();
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly InventoryService _sut;

    public InventoryServiceTests()
    {
        _sut = new InventoryService(_inventoryRepository, _orderRepository, _unitOfWork, _auditService);
    }

    private static global::Domain.Inventory.Aggregates.Inventory NewInventory(int stock = 10) =>
        new InventoryBuilder().WithInitialStock(stock).Build();

    [Fact]
    public async Task ReserveStockAsync_WhenInventoryIsMissing_ReturnsNotFound()
    {
        _inventoryRepository.GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((global::Domain.Inventory.Aggregates.Inventory?)null);

        var result = await _sut.ReserveStockAsync(
            VariantId.NewId(), StockQuantity.Create(2), "REF-1", ct: CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ReserveStockAsync_WhenStockIsSufficient_ReservesAndSaves()
    {
        var inventory = NewInventory(stock: 10);
        _inventoryRepository.GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.ReserveStockAsync(
            inventory.VariantId, StockQuantity.Create(3), "ORDER-1", ct: CancellationToken.None);

        result.ShouldBeSuccess();
        inventory.AvailableQuantity.ShouldBe(7);
        _inventoryRepository.Received(1).Update(inventory);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReserveStockAsync_WhenStockIsInsufficient_ReturnsFailureWithoutSaving()
    {
        var inventory = NewInventory(stock: 2);
        _inventoryRepository.GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.ReserveStockAsync(
            inventory.VariantId, StockQuantity.Create(5), "ORDER-1", ct: CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        _inventoryRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ReserveStockAsync_WhenConcurrencyConflict_ReturnsFailure()
    {
        var inventory = NewInventory(stock: 10);
        _inventoryRepository.GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Throws(new ConcurrencyException("conflict"));

        var result = await _sut.ReserveStockAsync(
            inventory.VariantId, StockQuantity.Create(2), "ORDER-1", ct: CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
    }

    [Fact]
    public async Task ReleaseReservationAsync_WhenReserved_ReleasesAndSaves()
    {
        var inventory = NewInventory(stock: 10);
        inventory.ReserveStock(StockQuantity.Create(4), "ORDER-7");
        _inventoryRepository.GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.ReleaseReservationAsync(
            inventory.VariantId, StockQuantity.Create(4), "ORDER-7", ct: CancellationToken.None);

        result.ShouldBeSuccess();
        inventory.AvailableQuantity.ShouldBe(10);
        _inventoryRepository.Received(1).Update(inventory);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReleaseReservationAsync_WhenInventoryIsMissing_ReturnsNotFound()
    {
        _inventoryRepository.GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((global::Domain.Inventory.Aggregates.Inventory?)null);

        var result = await _sut.ReleaseReservationAsync(
            VariantId.NewId(), StockQuantity.Create(1), "ORDER-1", ct: CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task AdjustStockAsync_WhenInventoryExists_AdjustsAndSaves()
    {
        var inventory = NewInventory(stock: 10);
        _inventoryRepository.GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.AdjustStockAsync(
            inventory.VariantId, StockQuantity.Create(5), UserId.NewId(), "recount", CancellationToken.None);

        result.ShouldBeSuccess();
        _inventoryRepository.Received(1).Update(inventory);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdjustStockAsync_WhenInventoryIsMissing_ReturnsNotFound()
    {
        _inventoryRepository.GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((global::Domain.Inventory.Aggregates.Inventory?)null);

        var result = await _sut.AdjustStockAsync(
            VariantId.NewId(), StockQuantity.Create(5), UserId.NewId(), "recount", CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task ReturnStockForOrderAsync_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        _orderRepository.FindByIdAsync(Arg.Any<global::Domain.Order.ValueObjects.OrderId>(), Arg.Any<CancellationToken>())
            .Returns((global::Domain.Order.Aggregates.Order?)null);

        var result = await _sut.ReturnStockForOrderAsync(
            global::Domain.Order.ValueObjects.OrderId.NewId(), Guid.NewGuid(), "return", CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task ReturnStockForOrderAsync_WhenOrderExists_ReturnsStockAndSaves()
    {
        var userId = UserId.NewId();
        var variantId = VariantId.NewId();
        var productId = global::Domain.Product.ValueObjects.ProductId.NewId();
        var order = new OrderBuilder()
            .WithUserId(userId)
            .WithItemSnapshots(new OrderItemSnapshotBuilder()
                .WithVariantId(variantId)
                .WithProductId(productId)
                .WithQuantity(2)
                .WithUnitPrice(100_000m, "IRT")
                .Build())
            .Build();
        order.ClearDomainEvents();
        var inventory = new InventoryBuilder().WithVariantId(variantId).WithInitialStock(8).Build();
        _orderRepository.FindByIdAsync(Arg.Any<global::Domain.Order.ValueObjects.OrderId>(), Arg.Any<CancellationToken>())
            .Returns(order);
        _inventoryRepository.GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.ReturnStockForOrderAsync(
            order.Id, Guid.NewGuid(), "customer return", CancellationToken.None);

        result.ShouldBeSuccess();
        _inventoryRepository.Received(1).Update(inventory);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnStockForOrderAsync_WhenSaveFails_LogsErrorAndReturnsFailure()
    {
        var userId = UserId.NewId();
        var order = new OrderBuilder().WithUserId(userId).Build();
        order.ClearDomainEvents();
        _orderRepository.FindByIdAsync(Arg.Any<global::Domain.Order.ValueObjects.OrderId>(), Arg.Any<CancellationToken>())
            .Returns(order);
        _inventoryRepository.GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((global::Domain.Inventory.Aggregates.Inventory?)null);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("db down"));

        var result = await _sut.ReturnStockForOrderAsync(
            order.Id, Guid.NewGuid(), "return", CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        await _auditService.Received(1).LogErrorAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RollbackReservationsAsync_WhenReservationsExist_ReleasesThem()
    {
        var inventory = NewInventory(stock: 10);
        inventory.ReserveStock(StockQuantity.Create(4), "ORDER-9");
        _inventoryRepository.GetByVariantIdsAsync(Arg.Any<IEnumerable<VariantId>>(), Arg.Any<CancellationToken>())
            .Returns([inventory]);

        var result = await _sut.RollbackReservationsAsync("ORDER-9", CancellationToken.None);

        result.ShouldBeSuccess();
        inventory.AvailableQuantity.ShouldBe(10);
        _inventoryRepository.Received(1).Update(inventory);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RollbackReservationsAsync_WhenNoReservations_DoesNothingButSaves()
    {
        _inventoryRepository.GetByVariantIdsAsync(Arg.Any<IEnumerable<VariantId>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _sut.RollbackReservationsAsync("UNKNOWN-REF", CancellationToken.None);

        result.ShouldBeSuccess();
        _inventoryRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RollbackReservationsAsync_WhenSaveFails_ReturnsFailure()
    {
        _inventoryRepository.GetByVariantIdsAsync(Arg.Any<IEnumerable<VariantId>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("db down"));

        var result = await _sut.RollbackReservationsAsync("REF", CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
    }
}
