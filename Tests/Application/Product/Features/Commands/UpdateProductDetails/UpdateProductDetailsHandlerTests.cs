using Application.Product.Features.Commands.UpdateProductDetails;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using DomainProduct = Domain.Product.Aggregates.Product;

namespace Tests.Application.Product.Features.Commands.UpdateProductDetails;

public class UpdateProductDetailsHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>(); private readonly UpdateProductDetailsHandler _sut;

    private static readonly byte[] RowVersionBytes = { 0, 0, 0, 0, 0, 0, 7, 209 };
    private static readonly string RowVersion = Convert.ToBase64String(RowVersionBytes);

    public UpdateProductDetailsHandlerTests()
    {
        _sut = new UpdateProductDetailsHandler(_productRepository);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ReturnsNotFoundAndDoesNotUpdate()
    {
        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns((DomainProduct?)null);

        var command = new UpdateProductDetailsCommand(
            Guid.NewGuid(),
            "New Name",
            "desc",
            Guid.NewGuid(),
            true,
            null,
            RowVersion);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _productRepository.DidNotReceiveWithAnyArgs().Update(default!, default);
    }

    [Fact]
    public async Task Handle_WhenSlugAlreadyExists_ReturnsConflictAndDoesNotUpdate()
    {
        var product = new ProductBuilder().Build();

        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(product);
        _productRepository
            .ExistsBySlugAsync(Arg.Any<ProductSlug>(), Arg.Any<ProductId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new UpdateProductDetailsCommand(
            product.Id.Value,
            "Duplicate Name",
            "desc",
            Guid.NewGuid(),
            true,
            null,
            RowVersion);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        _productRepository.DidNotReceiveWithAnyArgs().Update(default!, default);
    }

    [Fact]
    public async Task Handle_WhenProductExistsAndSlugAvailable_UpdatesProductAndReturnsSuccess()
    {
        var product = new ProductBuilder().Build();

        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(product);
        _productRepository
            .ExistsBySlugAsync(Arg.Any<ProductSlug>(), Arg.Any<ProductId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new UpdateProductDetailsCommand(
            product.Id.Value,
            "New Product Name",
            "new description",
            Guid.NewGuid(),
            true,
            null,
            RowVersion);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        product.Name.Value.ShouldBe("New Product Name");
        product.Description.ShouldBe("new description");
        _productRepository.Received(1).Update(product, Arg.Is<byte[]>(rv => rv.SequenceEqual(RowVersionBytes)));
    }

    [Fact]
    public async Task Handle_WhenIsActiveFalseAndProductWasActive_DeactivatesProduct()
    {
        var product = new ProductBuilder().Build();

        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(product);
        _productRepository
            .ExistsBySlugAsync(Arg.Any<ProductSlug>(), Arg.Any<ProductId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new UpdateProductDetailsCommand(
            product.Id.Value,
            "Another Name",
            null,
            Guid.NewGuid(),
            false,
            null,
            RowVersion);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        product.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_WhenIsActiveTrueAndProductWasInactive_ActivatesProduct()
    {
        var product = new ProductBuilder().Build();
        product.Deactivate();

        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(product);
        _productRepository
            .ExistsBySlugAsync(Arg.Any<ProductSlug>(), Arg.Any<ProductId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new UpdateProductDetailsCommand(
            product.Id.Value,
            "Reactivated Product",
            null,
            Guid.NewGuid(),
            true,
            null,
            RowVersion);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        product.IsActive.ShouldBeTrue();
    }
}
