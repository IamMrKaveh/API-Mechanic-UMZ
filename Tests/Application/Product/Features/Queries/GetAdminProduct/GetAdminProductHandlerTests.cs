using Application.Product.Contracts;
using Application.Product.Features.Queries.GetAdminProduct;
using Application.Product.Features.Shared;
using Domain.Product.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Product.Features.Queries.GetAdminProduct;

public class GetAdminProductHandlerTests
{
    private readonly IProductQueryService _productQueryService = Substitute.For<IProductQueryService>(); private readonly GetAdminProductHandler _sut;

    public GetAdminProductHandlerTests()
    {
        _sut = new GetAdminProductHandler(_productQueryService);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ReturnsNotFound()
    {
        _productQueryService
            .GetAdminProductDetailAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns((AdminProductDetailDto?)null);

        var result = await _sut.Handle(new GetAdminProductQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenProductExists_ReturnsSuccessWithDto()
    {
        var productId = Guid.NewGuid();
        var dto = new AdminProductDetailDto { Id = productId, Name = "Admin Product" };

        _productQueryService
            .GetAdminProductDetailAsync(Arg.Is<ProductId>(x => x.Value == productId), Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.Handle(new GetAdminProductQuery(productId), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(dto);
    }
}
