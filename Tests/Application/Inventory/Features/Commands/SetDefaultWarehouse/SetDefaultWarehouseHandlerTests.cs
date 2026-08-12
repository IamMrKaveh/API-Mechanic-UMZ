using Application.Inventory.Features.Commands.SetDefaultWarehouse;
using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Wh = Domain.Inventory.Aggregates.Warehouse;

namespace Tests.Application.Inventory.Features.Commands.SetDefaultWarehouse;

public class SetDefaultWarehouseHandlerTests
{
    private readonly IWarehouseRepository _warehouseRepository = Substitute.For<IWarehouseRepository>(); private readonly SetDefaultWarehouseHandler _sut;

    public SetDefaultWarehouseHandlerTests()
    {
        _sut = new SetDefaultWarehouseHandler(_warehouseRepository);
    }

    [Fact]
    public async Task Handle_WhenTargetWarehouseNotFound_ReturnsNotFound()
    {
        _warehouseRepository
            .GetByIdAsync(Arg.Any<WarehouseId>(), Arg.Any<CancellationToken>())
            .Returns((Wh?)null);

        var result = await _sut.Handle(
            new SetDefaultWarehouseCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenOtherDefaultExists_ClearsOldDefaultAndSetsNew()
    {
        var target = new WarehouseBuilder().Build();
        var currentDefault = new WarehouseBuilder().AsDefault().Build();

        _warehouseRepository
            .GetByIdAsync(Arg.Any<WarehouseId>(), Arg.Any<CancellationToken>())
            .Returns(target);
        _warehouseRepository
            .GetDefaultAsync(Arg.Any<CancellationToken>())
            .Returns(currentDefault);

        var result = await _sut.Handle(
            new SetDefaultWarehouseCommand(target.Id.Value),
            CancellationToken.None);

        result.ShouldBeSuccess();
        currentDefault.IsDefault.ShouldBeFalse();
        target.IsDefault.ShouldBeTrue();
        _warehouseRepository.Received(1).Update(currentDefault);
        _warehouseRepository.Received(1).Update(target);
    }

    [Fact]
    public async Task Handle_WhenNoCurrentDefault_SetsTargetAsDefault()
    {
        var target = new WarehouseBuilder().Build();

        _warehouseRepository
            .GetByIdAsync(Arg.Any<WarehouseId>(), Arg.Any<CancellationToken>())
            .Returns(target);
        _warehouseRepository
            .GetDefaultAsync(Arg.Any<CancellationToken>())
            .Returns((Wh?)null);

        var result = await _sut.Handle(
            new SetDefaultWarehouseCommand(target.Id.Value),
            CancellationToken.None);

        result.ShouldBeSuccess();
        target.IsDefault.ShouldBeTrue();
        _warehouseRepository.Received(1).Update(target);
    }
}
