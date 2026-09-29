using Application.Audit.Contracts;
using Application.Common.Interfaces;
using Application.Inventory.Features.Commands.AdjustStock;
using Domain.Inventory.Interfaces;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Inv = Domain.Inventory.Aggregates.Inventory;
using SharedKernel.Abstractions.Interfaces;
using NSubstitute;

namespace Tests.Application.Inventory.Features.Commands.AdjustStock;

public class AdjustStockHandlerTests : HandlerTestBase
{
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>(); private readonly AdjustStockHandler _sut;

    public AdjustStockHandlerTests()
    {
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _sut = new AdjustStockHandler(_inventoryRepository, AuditService, CurrentUserService, DateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenInventoryNotFound_ReturnsNotFound()
    {
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((Inv?)null);

        var result = await _sut.Handle(
            new AdjustStockCommand(Guid.NewGuid(), 3, "reason"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WithValidAdjustment_AppliesChangeUpdatesRepositoryAndAudits()
    {
        var inventory = new InventoryBuilder().WithInitialStock(10).Build();
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.Handle(
            new AdjustStockCommand(inventory.VariantId.Value, -3, "manual audit"),
            CancellationToken.None);

        result.ShouldBeSuccess();
        inventory.StockQuantity.Value.ShouldBe(7);
        _inventoryRepository.Received(1).Update(inventory);
        await AuditService.Received(1).LogInventoryEventAsync(
            inventory.VariantId,
            "AdjustStock",
            Arg.Any<string>(),
            Arg.Any<UserId?>());
    }

    [Fact]
    public async Task Handle_WhenInventoryIsUnlimited_ReturnsFailureAndDoesNotAuditOrUpdate()
    {
        var inventory = new InventoryBuilder().AsUnlimited().Build();
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.Handle(
            new AdjustStockCommand(inventory.VariantId.Value, 3, "reason"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        _inventoryRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await AuditService.DidNotReceiveWithAnyArgs().LogInventoryEventAsync(default!, default!, default!, default);
    }
}

