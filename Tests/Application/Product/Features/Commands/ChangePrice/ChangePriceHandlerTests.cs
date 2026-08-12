using Application.Cache.Contracts;
using Application.Product.Features.Commands.ChangePrice;
using Domain.Product.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Product.Features.Commands.ChangePrice;

public class ChangePriceHandlerTests
{
    private readonly IVariantRepository _variantRepository = Substitute.For<IVariantRepository>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly ChangePriceHandler _sut;

    public ChangePriceHandlerTests()
    {
        _sut = new ChangePriceHandler(_variantRepository, _cacheService);
    }

    [Fact]
    public async Task Handle_WhenVariantNotFound_ReturnsNotFoundAndDoesNotUpdate()
    {
        _variantRepository
            .GetByIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((ProductVariant?)null);

        var command = new ChangePriceCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            100_000m,
            120_000m);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _variantRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenVariantBelongsToDifferentProduct_ReturnsNotFoundAndDoesNotUpdate()
    {
        var variant = new ProductVariantBuilder()
            .WithProductId(ProductId.NewId())
            .Build();

        _variantRepository
            .GetByIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);

        var command = new ChangePriceCommand(
            Guid.NewGuid(),
            variant.Id.Value,
            Guid.NewGuid(),
            100_000m,
            120_000m);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _variantRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenVariantMatchesProduct_ChangesPriceUpdatesAndInvalidatesCaches()
    {
        var productId = ProductId.NewId();
        var variant = new ProductVariantBuilder()
            .WithProductId(productId)
            .WithSellingPrice(50_000m)
            .WithOriginalPrice(60_000m)
            .Build();

        _variantRepository
            .GetByIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);

        var command = new ChangePriceCommand(
            productId.Value,
            variant.Id.Value,
            Guid.NewGuid(),
            100_000m,
            120_000m);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        variant.SellingPrice.Amount.ShouldBe(100_000m);
        _variantRepository.Received(1).Update(variant);
        await _cacheService.Received(1).RemoveAsync($"product:{productId.Value}", Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveAsync($"variant:{variant.Id.Value}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOriginalPriceNotGreaterThanSelling_UsesSellingPriceAsCompareAtPrice()
    {
        var productId = ProductId.NewId();
        var variant = new ProductVariantBuilder()
            .WithProductId(productId)
            .WithSellingPrice(50_000m)
            .WithOriginalPrice(60_000m)
            .Build();

        _variantRepository
            .GetByIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);

        var command = new ChangePriceCommand(
            productId.Value,
            variant.Id.Value,
            Guid.NewGuid(),
            100_000m,
            100_000m);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        variant.SellingPrice.Amount.ShouldBe(100_000m);
        variant.OriginalPrice!.Amount.ShouldBe(100_000m);
    }
}
