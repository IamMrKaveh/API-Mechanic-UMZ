using Application.Inventory.Features.Queries.GetAllWarehouses;
using Domain.Inventory.Interfaces;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Wh = Domain.Inventory.Aggregates.Warehouse;

namespace Tests.Application.Inventory.Features.Queries.GetAllWarehouses;

public class GetAllWarehousesHandlerTests
{
    private readonly IWarehouseRepository _warehouseRepository = Substitute.For<IWarehouseRepository>(); private readonly GetAllWarehousesHandler _sut;

    public GetAllWarehousesHandlerTests()
    {
        _sut = new GetAllWarehousesHandler(_warehouseRepository);
    }

    [Fact]
    public async Task Handle_WhenNoWarehousesExist_ReturnsSuccessWithEmptyList()
    {
        _warehouseRepository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _sut.Handle(new GetAllWarehousesQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_WhenWarehousesExist_ReturnsSuccessWithMappedDtos()
    {
        var w1 = new WarehouseBuilder().WithCode("WH-01").WithName("Main").WithCity("Tehran").Build();
        var w2 = new WarehouseBuilder().WithCode("WH-02").WithName("Second").WithCity("Shiraz").AsDefault().Build();

        _warehouseRepository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Wh> { w1, w2 });

        var result = await _sut.Handle(new GetAllWarehousesQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Count.ShouldBe(2);
        result.Value.ShouldContain(d => d.Code == "WH-01" && d.Name == "Main");
        result.Value.ShouldContain(d => d.Code == "WH-02" && d.IsDefault);
    }
}
