using Application.Product.Contracts;
using Application.Product.Features.Queries.GetProduct;
using Application.Product.Features.Shared;
using Domain.Product.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Product.Features.Queries.GetProduct;

public class GetProductHandlerTests
{
    private readonly IProductQueryService _productQueryService = Substitute.For<IProductQueryService>(); private readonly GetProductHandler _sut;

    public GetProductHandlerTests()
    {
        _sut = new GetProductHandler(_productQueryService);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ReturnsNotFound()
    {
        _productQueryService
            .GetProductDetailAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns((ProductDetailDto?)null);

        var result = await _sut.Handle(new GetProductQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenProductExists_ReturnsSuccessWithDto()
    {
        var productId = Guid.NewGuid();
        var dto = new ProductDetailDto { Id = productId, Name = "Sample" };

        _productQueryService
            .GetProductDetailAsync(Arg.Is<ProductId>(x => x.Value == productId), Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.Handle(new GetProductQuery(productId), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(dto);
    }
}
