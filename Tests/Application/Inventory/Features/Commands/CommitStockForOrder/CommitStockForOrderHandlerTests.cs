using Application.Audit.Contracts;
using Application.Common.Interfaces;
using Application.Inventory.Features.Commands.CommitStockForOrder;
using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Inventory.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Inv = Domain.Inventory.Aggregates.Inventory;

namespace Tests.Application.Inventory.Features.Commands.CommitStockForOrder;

public class CommitStockForOrderHandlerTests
{
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly CommitStockForOrderHandler _sut;

    public CommitStockForOrderHandlerTests()
    {
        _unitOfWork
            .ExecuteStrategyAsync(
                Arg.Any<Func<CancellationToken, Task<int>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                var op = ci.Arg<Func<CancellationToken, Task<int>>>();
                return await op!(ci.Arg<CancellationToken>());
            });

        _sut = new CommitStockForOrderHandler(_inventoryRepository, _unitOfWork, _auditService);
    }

    [Fact]
    public async Task Handle_WithEmptyItems_ReturnsFailureAndDoesNotUseUnitOfWork()
    {
        var command = new CommitStockForOrderCommand(
            Array.Empty<OrderItemStockCommit>(),
            "ORDER-1");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        await _unitOfWork.DidNotReceiveWithAnyArgs().ExecuteStrategyAsync(default(Func<CancellationToken, Task<int>>)!, default);
    }

    [Fact]
    public async Task Handle_WithReservedInventory_ConfirmsReservationAndReturnsSuccess()
    {
        var inv = new InventoryBuilder().WithInitialStock(10).Build();
        InventoryDomainService.Reserve(inv, StockQuantity.Create(4), "ORDER-2").ShouldBeSuccess();

        _inventoryRepository
            .GetByVariantIdAsync(inv.VariantId, Arg.Any<CancellationToken>())
            .Returns(inv);

        var command = new CommitStockForOrderCommand(new[]
        {
        new OrderItemStockCommit(inv.VariantId.Value, 4, Guid.NewGuid())
    }, "ORDER-2");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        inv.ReservedQuantity.Value.ShouldBe(0);
        inv.StockQuantity.Value.ShouldBe(6);
        _inventoryRepository.Received(1).Update(inv);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogInventoryEventAsync(
            inv.VariantId,
            "CommitStockForOrder",
            Arg.Any<string>(),
            Arg.Any<UserId?>());
    }

    [Fact]
    public async Task Handle_WhenInventoryMissing_ThrowsDomainException()
    {
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((Inv?)null);

        var command = new CommitStockForOrderCommand(new[]
        {
        new OrderItemStockCommit(Guid.NewGuid(), 1, null)
    }, "ORDER-3");

        await Should.ThrowAsync<DomainException>(
            () => _sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenReservationInsufficient_ThrowsDomainException()
    {
        var inv = new InventoryBuilder().WithInitialStock(10).Build();
        _inventoryRepository
            .GetByVariantIdAsync(inv.VariantId, Arg.Any<CancellationToken>())
            .Returns(inv);

        var command = new CommitStockForOrderCommand(new[]
        {
        new OrderItemStockCommit(inv.VariantId.Value, 3, null)
    }, "ORDER-4");

        await Should.ThrowAsync<DomainException>(
            () => _sut.Handle(command, CancellationToken.None));
    }
}
