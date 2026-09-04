using Application.Cache.Contracts;
using Application.Inventory.Features.Commands.UpdateWarehouse;
using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Wh = Domain.Inventory.Aggregates.Warehouse;

namespace Tests.Application.Inventory.Features.Commands.UpdateWarehouse;

public class UpdateWarehouseHandlerTests
{
    private readonly IWarehouseRepository _warehouseRepository = Substitute.For<IWarehouseRepository>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly UpdateWarehouseHandler _sut;

    public UpdateWarehouseHandlerTests()
    {
        _sut = new UpdateWarehouseHandler(_warehouseRepository, _cacheService);
    }

    [Fact]
    public async Task Handle_WhenWarehouseNotFound_ReturnsNotFound()
    {
        _warehouseRepository
            .GetByIdAsync(Arg.Any<WarehouseId>(), Arg.Any<CancellationToken>())
            .Returns((Wh?)null);

        var result = await _sut.Handle(
            new UpdateWarehouseCommand(Guid.NewGuid(), "N", "C", null, null, 1),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveByPrefixAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WithExistingWarehouse_UpdatesFieldsAndReturnsSuccess()
    {
        var warehouse = new WarehouseBuilder().WithCode("WH-1").WithName("Old").WithCity("A").WithPriority(1).Build();
        _warehouseRepository
            .GetByIdAsync(Arg.Any<WarehouseId>(), Arg.Any<CancellationToken>())
            .Returns(warehouse);

        var result = await _sut.Handle(
            new UpdateWarehouseCommand(warehouse.Id.Value, "New", "B", "addr", "091", 9),
            CancellationToken.None);

        result.ShouldBeSuccess();
        warehouse.Name.ShouldBe("New");
        warehouse.City.ShouldBe("B");
        warehouse.Address.ShouldBe("addr");
        warehouse.Phone.ShouldBe("091");
        warehouse.Priority.ShouldBe(9);
        _warehouseRepository.Received(1).Update(warehouse);
        await _cacheService.Received(1).RemoveByPrefixAsync("warehouses:", Arg.Any<CancellationToken>());
    }
}
