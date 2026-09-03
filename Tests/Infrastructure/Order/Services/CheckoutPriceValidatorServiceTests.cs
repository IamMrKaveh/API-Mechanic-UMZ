using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.Interfaces;
using Infrastructure.Order.Services;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Infrastructure.Order.Services;

public class CheckoutPriceValidatorServiceTests
{
    private readonly IVariantRepository _variantRepository = Substitute.For<IVariantRepository>();
    private readonly CheckoutPriceValidatorService _sut;

    public CheckoutPriceValidatorServiceTests()
    {
        _sut = new CheckoutPriceValidatorService(_variantRepository);
    }

    private static OrderItemSnapshot NewSnapshot(decimal unitPrice) =>
        new OrderItemSnapshotBuilder()
            .WithUnitPrice(unitPrice, "IRT")
            .Build();

    private static ProductVariant NewVariant(OrderItemSnapshot snapshot, decimal sellingPrice) =>
        new ProductVariantBuilder()
            .WithId(snapshot.VariantId)
            .WithProductId(snapshot.ProductId)
            .WithSellingPrice(sellingPrice, "IRT")
            .Build();

    [Fact]
    public async Task ValidateAsync_WhenVariantNoLongerExists_SkipsItemAndSucceeds()
    {
        _variantRepository
            .GetByIdAsync(Arg.Any<global::Domain.Variant.ValueObjects.VariantId>(), Arg.Any<CancellationToken>())
            .Returns((ProductVariant?)null);

        var result = await _sut.ValidateAsync([NewSnapshot(100_000m)], CancellationToken.None);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task ValidateAsync_WhenPricesMatch_Succeeds()
    {
        var snapshot = NewSnapshot(100_000m);
        var variant = NewVariant(snapshot, 100_000m);
        _variantRepository
            .GetByIdAsync(snapshot.VariantId, Arg.Any<CancellationToken>())
            .Returns(variant);

        var result = await _sut.ValidateAsync([snapshot], CancellationToken.None);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task ValidateAsync_WhenPriceDriftIsWithinTolerance_Succeeds()
    {
        var snapshot = NewSnapshot(100_000m);
        var variant = NewVariant(snapshot, 100_001m);
        _variantRepository
            .GetByIdAsync(snapshot.VariantId, Arg.Any<CancellationToken>())
            .Returns(variant);

        var result = await _sut.ValidateAsync([snapshot], CancellationToken.None);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task ValidateAsync_WhenPriceChangedBeyondTolerance_ReturnsFailure()
    {
        var snapshot = NewSnapshot(100_000m);
        var variant = NewVariant(snapshot, 120_000m);
        _variantRepository
            .GetByIdAsync(snapshot.VariantId, Arg.Any<CancellationToken>())
            .Returns(variant);

        var result = await _sut.ValidateAsync([snapshot], CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
    }

    [Fact]
    public async Task ValidateAsync_WhenAnyItemChanged_ReturnsFailure()
    {
        var steady = NewSnapshot(50_000m);
        var changed = NewSnapshot(50_000m);
        var steadyVariant = NewVariant(steady, 50_000m);
        var changedVariant = NewVariant(changed, 60_000m);
        _variantRepository
            .GetByIdAsync(steady.VariantId, Arg.Any<CancellationToken>())
            .Returns(steadyVariant);
        _variantRepository
            .GetByIdAsync(changed.VariantId, Arg.Any<CancellationToken>())
            .Returns(changedVariant);

        var result = await _sut.ValidateAsync([steady, changed], CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
    }

    [Fact]
    public async Task ValidateAsync_WhenItemsAreEmpty_SucceedsWithoutRepositoryCall()
    {
        var result = await _sut.ValidateAsync([], CancellationToken.None);

        result.ShouldBeSuccess();
        await _variantRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default!, default);
    }
}
