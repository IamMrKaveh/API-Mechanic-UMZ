using Application.Cache.Contracts;
using Application.Inventory.Features.Commands.CreateWarehouse;
using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Wh = Domain.Inventory.Aggregates.Warehouse;

namespace Tests.Application.Inventory.Features.Commands.CreateWarehouse;

public class CreateWarehouseHandlerTests
{
    private readonly IWarehouseRepository _warehouseRepository = Substitute.For<IWarehouseRepository>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly CreateWarehouseHandler _sut;

    public CreateWarehouseHandlerTests()
    {
        _sut = new CreateWarehouseHandler(_warehouseRepository, _cacheService);
    }

    [Fact]
    public async Task Handle_WhenCodeAlreadyExists_ReturnsConflict()
    {
        _warehouseRepository
            .ExistsByCodeAsync("WH-01", Arg.Any<WarehouseId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(
            new CreateWarehouseCommand("WH-01", "Main", "Tehran", null, null, 1, false),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _warehouseRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveByPrefixAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WithValidCommandAndUniqueCode_ReturnsSuccessAndAddsWarehouse()
    {
        _warehouseRepository
            .ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<WarehouseId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.Handle(
            new CreateWarehouseCommand("WH-02", "Second", "Shiraz", "addr", "021", 5, false),
            CancellationToken.None);

        result.ShouldBeSuccess();
        await _warehouseRepository.Received(1).AddAsync(
            Arg.Is<Wh>(w => w!.Code == WarehouseCode.Create("WH-02") && w.Name == "Second" && !w.IsDefault),
            Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveByPrefixAsync("warehouses:", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenIsDefaultTrueAndCurrentDefaultExists_ClearsCurrentDefaultBeforeAdding()
    {
        var currentDefault = new WarehouseBuilder().WithCode("WH-EXIST").AsDefault().Build();

        _warehouseRepository
            .ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<WarehouseId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _warehouseRepository
            .GetDefaultAsync(Arg.Any<CancellationToken>())
            .Returns(currentDefault);

        var result = await _sut.Handle(
            new CreateWarehouseCommand("WH-NEW", "New", "Isfahan", null, null, 3, true),
            CancellationToken.None);

        result.ShouldBeSuccess();
        currentDefault.IsDefault.ShouldBeFalse();
        _warehouseRepository.Received(1).Update(currentDefault);
        await _warehouseRepository.Received(1).AddAsync(
            Arg.Is<Wh>(w => w!.IsDefault && w.Code == WarehouseCode.Create("WH-NEW")),
            Arg.Any<CancellationToken>());
    }
}
