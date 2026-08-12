using Application.Product.Contracts;
using Application.Product.Features.Queries.GetProductDetails;
using Application.Product.Features.Shared;
using Domain.Product.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Product.Features.Queries.GetProductDetails;

public class GetProductDetailsHandlerTests
{
    private readonly IProductQueryService _productQueryService = Substitute.For<IProductQueryService>(); private readonly GetProductDetailsHandler _sut;

    public GetProductDetailsHandlerTests()
    {
        _sut = new GetProductDetailsHandler(_productQueryService);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ReturnsNotFound()
    {
        _productQueryService
            .GetPublicProductDetailAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns((PublicProductDetailDto?)null);

        var result = await _sut.Handle(new GetProductDetailsQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenProductExists_ReturnsSuccessWithDto()
    {
        var productId = Guid.NewGuid();
        var dto = new PublicProductDetailDto { Id = productId, Name = "Sample" };

        _productQueryService
            .GetPublicProductDetailAsync(Arg.Is<ProductId>(x => x.Value == productId), Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.Handle(new GetProductDetailsQuery(productId), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(dto);
    }
}
