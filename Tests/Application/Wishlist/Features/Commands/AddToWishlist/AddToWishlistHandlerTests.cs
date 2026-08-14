using Application.Common.Interfaces;
using Application.Wishlist.Features.Commands.AddToWishlist;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.Interfaces;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Products = Domain.Product.Aggregates.Product;
using Wishlists = Domain.Wishlist.Aggregates.Wishlist;

namespace Tests.Application.Wishlist.Features.Commands.AddToWishlist;

public class AddToWishlistHandlerTests
{
    private readonly IWishlistRepository _wishlistRepository = Substitute.For<IWishlistRepository>(); private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly AddToWishlistHandler _sut;

    public AddToWishlistHandlerTests()
    {
        _sut = new AddToWishlistHandler(_wishlistRepository, _productRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ReturnsNotFound()
    {
        var userGuid = Guid.NewGuid();
        var productGuid = Guid.NewGuid();

        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns((Products?)null);

        var result = await _sut.Handle(
            new AddToWishlistCommand(userGuid, productGuid),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        await _wishlistRepository.DidNotReceive().AddAsync(Arg.Any<Wishlists>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenProductAlreadyInWishlist_ReturnsConflict()
    {
        var userGuid = Guid.NewGuid();
        var productGuid = Guid.NewGuid();
        var product = new ProductBuilder().Build();

        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(product);
        _wishlistRepository
            .ExistsAsync(Arg.Any<UserId>(), Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(
            new AddToWishlistCommand(userGuid, productGuid),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _wishlistRepository.DidNotReceive().AddAsync(Arg.Any<Wishlists>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenProductActiveAndNotAlreadyInWishlist_AddsItemAndSavesAndReturnsSuccess()
    {
        var userGuid = Guid.NewGuid();
        var productGuid = Guid.NewGuid();
        var product = new ProductBuilder().Build();

        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(product);
        _wishlistRepository
            .ExistsAsync(Arg.Any<UserId>(), Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        Wishlists? added = null;
        _wishlistRepository
            .When(x => x.AddAsync(Arg.Any<Wishlists>(), Arg.Any<CancellationToken>()))
            .Do(ci => added = ci.Arg<Wishlists>());

        var result = await _sut.Handle(
            new AddToWishlistCommand(userGuid, productGuid),
            CancellationToken.None);

        result.ShouldBeSuccess();
        added.ShouldNotBeNull();
        added!.UserId.Value.ShouldBe(userGuid);
        added.ProductId.Value.ShouldBe(productGuid);
        await _wishlistRepository.Received(1).AddAsync(Arg.Any<Wishlists>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCalled_LooksUpProductAndExistenceWithConvertedIds()
    {
        var userGuid = Guid.NewGuid();
        var productGuid = Guid.NewGuid();
        var product = new ProductBuilder().Build();

        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(product);
        _wishlistRepository
            .ExistsAsync(Arg.Any<UserId>(), Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.Handle(
            new AddToWishlistCommand(userGuid, productGuid),
            CancellationToken.None);

        result.ShouldBeSuccess();
        await _productRepository.Received(1).GetByIdAsync(
            Arg.Is<ProductId>(p => p == ProductId.From(productGuid)),
            Arg.Any<CancellationToken>());
        await _wishlistRepository.Received(1).ExistsAsync(
            Arg.Is<UserId>(u => u == UserId.From(userGuid)),
            Arg.Is<ProductId>(p => p == ProductId.From(productGuid)),
            Arg.Any<CancellationToken>());
    }
}
