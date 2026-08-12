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

namespace Tests.Application.Inventory.Features.Commands.AdjustStock;

public class AdjustStockHandlerTests
{
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly AdjustStockHandler _sut;

    public AdjustStockHandlerTests()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _sut = new AdjustStockHandler(_inventoryRepository, _auditService, _currentUserService);
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
        await _auditService.Received(1).LogInventoryEventAsync(
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
        await _auditService.DidNotReceiveWithAnyArgs().LogInventoryEventAsync(default!, default!, default!, default);
    }
}
