using Application.Inventory.Contracts;
using Application.Inventory.Features.Queries.GetInventoryStatus;
using Application.Inventory.Features.Shared;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Inventory.Features.Queries.GetInventoryStatus;

public class GetInventoryStatusHandlerTests
{
    private readonly IInventoryQueryService _queryService = Substitute.For<IInventoryQueryService>(); private readonly IVariantRepository _variantRepository = Substitute.For<IVariantRepository>(); private readonly GetInventoryStatusHandler _sut;

    public GetInventoryStatusHandlerTests()
    {
        _sut = new GetInventoryStatusHandler(_queryService, _variantRepository);
    }

    [Fact]
    public async Task Handle_WhenStatusFoundInProjection_ReturnsSuccessWithProjection()
    {
        var variantId = Guid.NewGuid();
        var status = new InventoryStatusDto
        {
            VariantId = variantId,
            StockQuantity = 5,
            ReservedQuantity = 1,
            AvailableStock = 4,
            IsInStock = true
        };

        _queryService
            .GetInventoryStatusAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(status);

        var result = await _sut.Handle(new GetInventoryStatusQuery(variantId), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(status);
        await _variantRepository.DidNotReceiveWithAnyArgs().ExistsAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenStatusMissingButVariantExists_ReturnsSuccessWithEmptyStatus()
    {
        var variantId = Guid.NewGuid();

        _queryService
            .GetInventoryStatusAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((InventoryStatusDto?)null);
        _variantRepository
            .ExistsAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(new GetInventoryStatusQuery(variantId), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.VariantId.ShouldBe(variantId);
        result.Value.StockQuantity.ShouldBe(0);
        result.Value.ReservedQuantity.ShouldBe(0);
        result.Value.AvailableStock.ShouldBe(0);
        result.Value.IsInStock.ShouldBeFalse();
        result.Value.IsUnlimited.ShouldBeFalse();
        result.Value.IsLowStock.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_WhenStatusMissingAndVariantDoesNotExist_ReturnsNotFound()
    {
        _queryService
            .GetInventoryStatusAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((InventoryStatusDto?)null);
        _variantRepository
            .ExistsAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.Handle(new GetInventoryStatusQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }
}
