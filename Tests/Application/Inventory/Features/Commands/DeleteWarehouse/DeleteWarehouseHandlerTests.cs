using Application.Inventory.Features.Commands.DeleteWarehouse;
using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Wh = Domain.Inventory.Aggregates.Warehouse;

namespace Tests.Application.Inventory.Features.Commands.DeleteWarehouse;

public class DeleteWarehouseHandlerTests
{
    private readonly IWarehouseRepository _warehouseRepository = Substitute.For<IWarehouseRepository>(); private readonly DeleteWarehouseHandler _sut;

    public DeleteWarehouseHandlerTests()
    {
        _sut = new DeleteWarehouseHandler(_warehouseRepository);
    }

    [Fact]
    public async Task Handle_WhenWarehouseNotFound_ReturnsNotFound()
    {
        _warehouseRepository
            .GetByIdAsync(Arg.Any<WarehouseId>(), Arg.Any<CancellationToken>())
            .Returns((Wh?)null);

        var result = await _sut.Handle(
            new DeleteWarehouseCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _warehouseRepository.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    [Fact]
    public async Task Handle_WhenWarehouseIsDefault_ReturnsFailureAndDoesNotRemove()
    {
        var warehouse = new WarehouseBuilder().AsDefault().Build();
        _warehouseRepository
            .GetByIdAsync(Arg.Any<WarehouseId>(), Arg.Any<CancellationToken>())
            .Returns(warehouse);

        var result = await _sut.Handle(
            new DeleteWarehouseCommand(warehouse.Id.Value),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        _warehouseRepository.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    [Fact]
    public async Task Handle_WithNonDefaultWarehouse_RemovesAndReturnsSuccess()
    {
        var warehouse = new WarehouseBuilder().Build();
        _warehouseRepository
            .GetByIdAsync(Arg.Any<WarehouseId>(), Arg.Any<CancellationToken>())
            .Returns(warehouse);

        var result = await _sut.Handle(
            new DeleteWarehouseCommand(warehouse.Id.Value),
            CancellationToken.None);

        result.ShouldBeSuccess();
        _warehouseRepository.Received(1).Remove(warehouse);
    }
}
