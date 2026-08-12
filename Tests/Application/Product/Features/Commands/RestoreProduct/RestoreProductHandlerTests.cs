using Application.Cache.Contracts;
using Application.Product.Features.Commands.RestoreProduct;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using DomainProduct = Domain.Product.Aggregates.Product;

namespace Tests.Application.Product.Features.Commands.RestoreProduct;

public class RestoreProductHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly RestoreProductHandler _sut;

    public RestoreProductHandlerTests()
    {
        _sut = new RestoreProductHandler(_productRepository, _cacheService);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ReturnsNotFoundAndDoesNotInvalidateCache()
    {
        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns((DomainProduct?)null);

        var result = await _sut.Handle(new RestoreProductCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _productRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenProductExists_RestoresProductUpdatesAndInvalidatesCache()
    {
        var product = new ProductBuilder().Build();

        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(product);

        var command = new RestoreProductCommand(product.Id.Value);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        _productRepository.Received(1).Update(product);
        await _cacheService.Received(1).RemoveAsync($"product:{product.Id.Value}", Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveAsync($"brand:{product.BrandId}", Arg.Any<CancellationToken>());
    }
}
