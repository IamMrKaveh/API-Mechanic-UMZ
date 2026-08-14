using Application.Variant.Contracts;
using Application.Variant.Features.Queries.GetVariantShipping;
using Application.Variant.Features.Shared;
using Domain.Variant.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Variant.Features.Queries.GetVariantShipping;

public class GetVariantShippingHandlerTests
{
    private readonly IVariantQueryService _variantQueryService = Substitute.For<IVariantQueryService>(); private readonly GetVariantShippingHandler _sut;

    public GetVariantShippingHandlerTests()
    {
        _sut = new GetVariantShippingHandler(_variantQueryService);
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsNull_ReturnsNotFound()
    {
        _variantQueryService
            .GetVariantShippingInfoAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((VariantShippingInfoDto?)null);

        var query = new GetVariantShippingQuery(Guid.NewGuid());

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsDto_ReturnsSuccessWithDto()
    {
        var dto = new VariantShippingInfoDto();
        _variantQueryService
            .GetVariantShippingInfoAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(dto);

        var query = new GetVariantShippingQuery(Guid.NewGuid());

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task Handle_ForwardsVariantIdToQueryService()
    {
        _variantQueryService
            .GetVariantShippingInfoAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(new VariantShippingInfoDto());

        var variantGuid = Guid.NewGuid();
        var query = new GetVariantShippingQuery(variantGuid);

        _ = await _sut.Handle(query, CancellationToken.None);

        await _variantQueryService.Received(1).GetVariantShippingInfoAsync(
            Arg.Is<VariantId>(v => v == VariantId.From(variantGuid)),
            Arg.Any<CancellationToken>());
    }
}
