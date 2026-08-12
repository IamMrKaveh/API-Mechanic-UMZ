using Application.Inventory.Features.Queries.GetWarehouseById;
using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Wh = Domain.Inventory.Aggregates.Warehouse;

namespace Tests.Application.Inventory.Features.Queries.GetWarehouseById;

public class GetWarehouseByIdHandlerTests
{
    private readonly IWarehouseRepository _warehouseRepository = Substitute.For<IWarehouseRepository>(); private readonly GetWarehouseByIdHandler _sut;

    public GetWarehouseByIdHandlerTests()
    {
        _sut = new GetWarehouseByIdHandler(_warehouseRepository);
    }

    [Fact]
    public async Task Handle_WhenWarehouseNotFound_ReturnsNotFound()
    {
        _warehouseRepository
            .GetByIdAsync(Arg.Any<WarehouseId>(), Arg.Any<CancellationToken>())
            .Returns((Wh?)null);

        var result = await _sut.Handle(new GetWarehouseByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenWarehouseExists_ReturnsSuccessWithMappedDto()
    {
        var warehouse = new WarehouseBuilder()
            .WithCode("WH-100")
            .WithName("Central")
            .WithCity("Isfahan")
            .WithPriority(7)
            .Build();

        _warehouseRepository
            .GetByIdAsync(Arg.Any<WarehouseId>(), Arg.Any<CancellationToken>())
            .Returns(warehouse);

        var result = await _sut.Handle(new GetWarehouseByIdQuery(warehouse.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Id.ShouldBe(warehouse.Id.Value);
        result.Value.Code.ShouldBe("WH-100");
        result.Value.Name.ShouldBe("Central");
        result.Value.City.ShouldBe("Isfahan");
        result.Value.Priority.ShouldBe(7);
    }
}
