using Application.Audit.Contracts;
using Application.Common.Interfaces;
using Application.Inventory.Features.Commands.AddStock;
using Domain.Inventory.Interfaces;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Inv = Domain.Inventory.Aggregates.Inventory;

namespace Tests.Application.Inventory.Features.Commands.AddStock;

public class AddStockHandlerTests
{
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly AddStockHandler _sut;

    public AddStockHandlerTests()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _sut = new AddStockHandler(_inventoryRepository, _auditService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenInventoryNotFound_ReturnsNotFoundAndDoesNotUpdateOrAudit()
    {
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((Inv?)null);

        var result = await _sut.Handle(
            new AddStockCommand(Guid.NewGuid(), 5, "restock"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _inventoryRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _auditService.DidNotReceiveWithAnyArgs().LogInventoryEventAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task Handle_WithExistingInventory_IncreasesStockAndReturnsSuccess()
    {
        var inventory = new InventoryBuilder().WithInitialStock(10).Build();
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.Handle(
            new AddStockCommand(inventory.VariantId.Value, 4, "restock"),
            CancellationToken.None);

        result.ShouldBeSuccess();
        inventory.StockQuantity.Value.ShouldBe(14);
        _inventoryRepository.Received(1).Update(inventory);
        await _auditService.Received(1).LogInventoryEventAsync(
            inventory.VariantId,
            "AddStock",
            Arg.Any<string>(),
            Arg.Any<UserId?>());
    }
}
