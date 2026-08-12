using Application.Product.Features.Commands.DeleteProduct;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using DomainProduct = Domain.Product.Aggregates.Product;

namespace Tests.Application.Product.Features.Commands.DeleteProduct;

public class DeleteProductHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>(); private readonly DeleteProductHandler _sut;

    public DeleteProductHandlerTests()
    {
        _sut = new DeleteProductHandler(_productRepository);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ReturnsNotFoundAndDoesNotUpdate()
    {
        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns((DomainProduct?)null);

        var result = await _sut.Handle(new DeleteProductCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _productRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenProductExists_DeactivatesProductAndReturnsSuccess()
    {
        var product = new ProductBuilder().Build();

        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(product);

        var result = await _sut.Handle(new DeleteProductCommand(product.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        product.IsActive.ShouldBeFalse();
        _productRepository.Received(1).Update(product);
    }
}
