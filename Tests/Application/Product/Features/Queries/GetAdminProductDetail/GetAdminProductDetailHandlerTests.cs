using Application.Product.Contracts;
using Application.Product.Features.Queries.GetAdminProductDetail;
using Application.Product.Features.Shared;
using Domain.Product.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Product.Features.Queries.GetAdminProductDetail;

public class GetAdminProductDetailHandlerTests
{
    private readonly IProductQueryService _productQueryService = Substitute.For<IProductQueryService>(); private readonly GetAdminProductDetailHandler _sut;

    public GetAdminProductDetailHandlerTests()
    {
        _sut = new GetAdminProductDetailHandler(_productQueryService);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ReturnsNotFound()
    {
        _productQueryService
            .GetAdminProductDetailAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns((AdminProductDetailDto?)null);

        var result = await _sut.Handle(new GetAdminProductDetailQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenProductExists_ReturnsSuccessWithDto()
    {
        var productId = Guid.NewGuid();
        var dto = new AdminProductDetailDto { Id = productId, Name = "Detail" };

        _productQueryService
            .GetAdminProductDetailAsync(Arg.Is<ProductId>(x => x.Value == productId), Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.Handle(new GetAdminProductDetailQuery(productId), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(dto);
    }
}
