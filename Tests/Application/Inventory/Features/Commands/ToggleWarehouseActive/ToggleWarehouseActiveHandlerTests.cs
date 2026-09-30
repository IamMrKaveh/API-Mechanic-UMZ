using Application.Inventory.Features.Commands.ToggleWarehouseActive;
using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Wh = Domain.Inventory.Aggregates.Warehouse;
using SharedKernel.Abstractions.Interfaces;
using NSubstitute;

namespace Tests.Application.Inventory.Features.Commands.ToggleWarehouseActive;

public class ToggleWarehouseActiveHandlerTests : HandlerTestBase
{
    private readonly IWarehouseRepository _warehouseRepository = Substitute.For<IWarehouseRepository>(); private readonly ToggleWarehouseActiveHandler _sut;

    public ToggleWarehouseActiveHandlerTests()
    {
        _sut = new ToggleWarehouseActiveHandler(_warehouseRepository, DateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenWarehouseNotFound_ReturnsNotFound()
    {
        _warehouseRepository
            .GetByIdAsync(Arg.Any<WarehouseId>(), Arg.Any<CancellationToken>())
            .Returns((Wh?)null);

        var result = await _sut.Handle(
            new ToggleWarehouseActiveCommand(Guid.NewGuid(), true),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WithIsActiveFalse_DeactivatesWarehouse()
    {
        var warehouse = new WarehouseBuilder().Build();
        _warehouseRepository
            .GetByIdAsync(Arg.Any<WarehouseId>(), Arg.Any<CancellationToken>())
            .Returns(warehouse);

        var result = await _sut.Handle(
            new ToggleWarehouseActiveCommand(warehouse.Id.Value, false),
            CancellationToken.None);

        result.ShouldBeSuccess();
        warehouse.IsActive.ShouldBeFalse();
        _warehouseRepository.Received(1).Update(warehouse);
    }

    [Fact]
    public async Task Handle_WithIsActiveTrueOnDeactivatedWarehouse_ActivatesWarehouse()
    {
        var warehouse = new WarehouseBuilder().Build();
        warehouse.Deactivate(DateTime.UtcNow);
        _warehouseRepository
            .GetByIdAsync(Arg.Any<WarehouseId>(), Arg.Any<CancellationToken>())
            .Returns(warehouse);

        var result = await _sut.Handle(
            new ToggleWarehouseActiveCommand(warehouse.Id.Value, true),
            CancellationToken.None);

        result.ShouldBeSuccess();
        warehouse.IsActive.ShouldBeTrue();
        _warehouseRepository.Received(1).Update(warehouse);
    }
}


