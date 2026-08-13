using Application.Brand.Contracts;
using Application.Brand.Features.Queries.GetBrand;
using Application.Brand.Features.Shared;
using Domain.Brand.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Brand.Features.Queries.GetBrand;

public class GetBrandHandlerTests
{
    private readonly IBrandQueryService _queryService = Substitute.For<IBrandQueryService>(); private readonly GetBrandHandler _sut;

    public GetBrandHandlerTests()
    {
        _sut = new GetBrandHandler(_queryService);
    }

    [Fact]
    public async Task Handle_WhenBrandNotFound_ReturnsNotFound()
    {
        _queryService
            .GetBrandDetailAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>())
            .Returns((BrandDetailDto?)null);

        var result = await _sut.Handle(new GetBrandQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenBrandFound_ReturnsSuccessWithDto()
    {
        var expected = new BrandDetailDto { Id = Guid.NewGuid(), Name = "Sony" };
        _queryService
            .GetBrandDetailAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetBrandQuery(expected.Id), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_PassesBrandIdBuiltFromRequestIdToQueryService()
    {
        var id = Guid.NewGuid();
        BrandId? captured = null;
        _queryService
            .GetBrandDetailAsync(Arg.Do<BrandId>(x => captured = x), Arg.Any<CancellationToken>())
            .Returns((BrandDetailDto?)null);

        _ = await _sut.Handle(new GetBrandQuery(id), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(id);
    }
}
