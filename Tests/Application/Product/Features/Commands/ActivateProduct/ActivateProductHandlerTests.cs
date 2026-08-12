using Application.Product.Features.Commands.ActivateProduct;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using DomainProduct = Domain.Product.Aggregates.Product;

namespace Tests.Application.Product.Features.Commands.ActivateProduct;

public class ActivateProductHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>(); private readonly ActivateProductHandler _sut;

    public ActivateProductHandlerTests()
    {
        _sut = new ActivateProductHandler(_productRepository);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ReturnsNotFoundAndDoesNotUpdate()
    {
        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns((DomainProduct?)null);

        var result = await _sut.Handle(new ActivateProductCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _productRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenProductAlreadyActive_ReturnsConflictAndDoesNotUpdate()
    {
        var product = new ProductBuilder().Build();

        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(product);

        var result = await _sut.Handle(new ActivateProductCommand(product.Id.Value), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        _productRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenProductIsInactive_ActivatesProductAndReturnsSuccess()
    {
        var product = new ProductBuilder().Build();
        product.Deactivate();

        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(product);

        var result = await _sut.Handle(new ActivateProductCommand(product.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        product.IsActive.ShouldBeTrue();
        _productRepository.Received(1).Update(product);
    }
}
