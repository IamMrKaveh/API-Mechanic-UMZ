using Application.Variant.Contracts;
using Application.Variant.Features.Queries.GetVariants;
using Application.Variant.Features.Shared;
using Domain.Product.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Variant.Features.Queries.GetVariants;

public class GetVariantsHandlerTests
{
    private readonly IVariantQueryService _variantQueryService = Substitute.For<IVariantQueryService>(); private readonly GetVariantsHandler _sut;

    public GetVariantsHandlerTests()
    {
        _sut = new GetVariantsHandler(_variantQueryService);
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsItems_ReturnsSuccessWithSameItems()
    {
        var items = new List<ProductVariantViewDto>
    {
        new() { Id = Guid.NewGuid(), Sku = "A" },
        new() { Id = Guid.NewGuid(), Sku = "B" }
    };
        _variantQueryService
            .GetProductVariantsAsync(Arg.Any<ProductId>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(items);

        var query = new GetVariantsQuery(Guid.NewGuid(), ActiveOnly: true);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(items);
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsEmpty_ReturnsSuccessWithEmpty()
    {
        _variantQueryService
            .GetProductVariantsAsync(Arg.Any<ProductId>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProductVariantViewDto>());

        var query = new GetVariantsQuery(Guid.NewGuid(), ActiveOnly: false);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ForwardsProductIdAndActiveOnlyFlagToQueryService()
    {
        _variantQueryService
            .GetProductVariantsAsync(Arg.Any<ProductId>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProductVariantViewDto>());

        var productGuid = Guid.NewGuid();
        var query = new GetVariantsQuery(productGuid, ActiveOnly: false);

        _ = await _sut.Handle(query, CancellationToken.None);

        await _variantQueryService.Received(1).GetProductVariantsAsync(
            Arg.Is<ProductId>(p => p!.Value == productGuid),
            false,
            Arg.Any<CancellationToken>());
    }
}
