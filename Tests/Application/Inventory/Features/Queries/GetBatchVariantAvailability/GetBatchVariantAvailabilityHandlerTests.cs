using Application.Inventory.Contracts;
using Application.Inventory.Features.Queries.GetBatchVariantAvailability;
using Application.Inventory.Features.Shared;
using Domain.Variant.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Inventory.Features.Queries.GetBatchVariantAvailability;

public class GetBatchVariantAvailabilityHandlerTests
{
    private readonly IInventoryQueryService _inventoryQueryService = Substitute.For<IInventoryQueryService>(); private readonly GetBatchVariantAvailabilityHandler _sut;

    public GetBatchVariantAvailabilityHandlerTests()
    {
        _sut = new GetBatchVariantAvailabilityHandler(_inventoryQueryService);
    }

    [Fact]
    public async Task Handle_WhenVariantIdsCollectionIsEmpty_ReturnsSuccessWithEmptyListAndDoesNotCallService()
    {
        var result = await _sut.Handle(
            new GetBatchVariantAvailabilityQuery(Array.Empty<Guid>()),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeEmpty();
        await _inventoryQueryService.DidNotReceiveWithAnyArgs().GetBatchAvailabilityAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenVariantIdsProvided_ReturnsSuccessWithServiceResult()
    {
        var expected = new List<VariantAvailabilityDto>
    {
        new() { VariantId = Guid.NewGuid(), IsAvailable = true, AvailableQuantity = 5 }
    };

        _inventoryQueryService
            .GetBatchAvailabilityAsync(Arg.Any<ICollection<VariantId>>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(
            new GetBatchVariantAvailabilityQuery(new[] { Guid.NewGuid(), Guid.NewGuid() }),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(expected);
    }
}
