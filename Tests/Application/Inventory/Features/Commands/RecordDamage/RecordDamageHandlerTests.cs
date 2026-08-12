using Application.Common.Interfaces;
using Application.Inventory.Features.Commands.RecordDamage;
using Domain.Inventory.Interfaces;
using Domain.Variant.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Inv = Domain.Inventory.Aggregates.Inventory;

namespace Tests.Application.Inventory.Features.Commands.RecordDamage;

public class RecordDamageHandlerTests
{
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly RecordDamageHandler _sut;

    public RecordDamageHandlerTests()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _sut = new RecordDamageHandler(_inventoryRepository, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenInventoryNotFound_ReturnsNotFound()
    {
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((Inv?)null);

        var result = await _sut.Handle(
            new RecordDamageCommand(Guid.NewGuid(), 1, "broken"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenInventoryIsUnlimited_ReturnsFailure()
    {
        var inventory = new InventoryBuilder().AsUnlimited().Build();
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.Handle(
            new RecordDamageCommand(inventory.VariantId.Value, 1, "broken"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        _inventoryRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WithSufficientStock_DecreasesStockAndReturnsSuccess()
    {
        var inventory = new InventoryBuilder().WithInitialStock(10).Build();
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.Handle(
            new RecordDamageCommand(inventory.VariantId.Value, 3, "broken"),
            CancellationToken.None);

        result.ShouldBeSuccess();
        inventory.StockQuantity.Value.ShouldBe(7);
        _inventoryRepository.Received(1).Update(inventory);
    }
}
