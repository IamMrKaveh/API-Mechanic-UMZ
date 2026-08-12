using Application.Audit.Contracts;
using Application.Common.Interfaces;
using Application.Inventory.Features.Commands.ReconcileStock;
using Domain.Inventory.Interfaces;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Inv = Domain.Inventory.Aggregates.Inventory;

namespace Tests.Application.Inventory.Features.Commands.ReconcileStock;

public class ReconcileStockHandlerTests
{
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly ReconcileStockHandler _sut;

    public ReconcileStockHandlerTests()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _sut = new ReconcileStockHandler(_inventoryRepository, _auditService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenInventoryNotFound_ReturnsNotFound()
    {
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((Inv?)null);

        var result = await _sut.Handle(
            new ReconcileStockCommand(Guid.NewGuid(), 5),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WithDifferenceFromCurrentStock_SetsStockAndAudits()
    {
        var inventory = new InventoryBuilder().WithInitialStock(10).Build();
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.Handle(
            new ReconcileStockCommand(inventory.VariantId.Value, 15),
            CancellationToken.None);

        result.ShouldBeSuccess();
        inventory.StockQuantity.Value.ShouldBe(15);
        _inventoryRepository.Received(1).Update(inventory);
        await _auditService.Received(1).LogInventoryEventAsync(
            inventory.VariantId,
            "ReconcileStock",
            Arg.Any<string>(),
            Arg.Any<UserId?>());
    }
}
