using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Shipping.Aggregates;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.Interfaces;
using Infrastructure.Order.Services;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Infrastructure.Order.Services;

public class CheckoutShippingValidatorServiceTests
{
    private readonly IShippingRepository _shippingRepository = Substitute.For<IShippingRepository>();
    private readonly IVariantRepository _variantRepository = Substitute.For<IVariantRepository>();
    private readonly CheckoutShippingValidatorService _sut;

    public CheckoutShippingValidatorServiceTests()
    {
        _sut = new CheckoutShippingValidatorService(_shippingRepository, _variantRepository);
    }

    private static OrderItemSnapshot NewSnapshot(ProductVariant variant, int quantity = 1) =>
        new OrderItemSnapshotBuilder()
            .WithVariantId(variant.Id)
            .WithProductId(variant.ProductId)
            .WithQuantity(quantity)
            .Build();

    private static ProductVariant NewVariant() => new ProductVariantBuilder().Build();

    [Fact]
    public async Task ValidateAndCalculateCostAsync_WhenShippingNotFound_ReturnsNotFound()
    {
        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns((global::Domain.Shipping.Aggregates.Shipping?)null);

        var result = await _sut.ValidateAndCalculateCostAsync(
            Guid.NewGuid(), 500_000m, [], CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task ValidateAndCalculateCostAsync_WhenShippingIsInactive_ReturnsFailure()
    {
        var shipping = new ShippingBuilder()
            .WithName($"Inactive Ship {Guid.NewGuid():N}"[..22])
            .AsDeleted()
            .Build();
        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns(shipping);

        var result = await _sut.ValidateAndCalculateCostAsync(
            shipping.Id.Value, 500_000m, [], CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
    }

    [Fact]
    public async Task ValidateAndCalculateCostAsync_WhenItemsAreEmpty_UsesFallbackCost()
    {
        var shipping = new ShippingBuilder()
            .WithName($"Fallback Ship {Guid.NewGuid():N}"[..22])
            .WithBaseCost(80_000m, "IRT")
            .Build();
        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns(shipping);

        var result = await _sut.ValidateAndCalculateCostAsync(
            shipping.Id.Value, 500_000m, [], CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Amount.ShouldBe(shipping.CalculateCost(Money.FromDecimal(500_000m)).Amount);
        await _variantRepository.DidNotReceiveWithAnyArgs().GetByIdsWithShippingsAsync(default!, default);
    }

    [Fact]
    public async Task ValidateAndCalculateCostAsync_WhenVariantHasNoAssignment_UsesMultiplierOne()
    {
        var shipping = new ShippingBuilder()
            .WithName($"Plain Ship {Guid.NewGuid():N}"[..19])
            .WithBaseCost(80_000m, "IRT")
            .Build();
        var variant = NewVariant();
        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns(shipping);
        _variantRepository
            .GetByIdsWithShippingsAsync(Arg.Any<IEnumerable<global::Domain.Variant.ValueObjects.VariantId>>(), Arg.Any<CancellationToken>())
            .Returns([variant]);

        var items = new[] { NewSnapshot(variant, quantity: 2) };
        var result = await _sut.ValidateAndCalculateCostAsync(
            shipping.Id.Value, 500_000m, items, CancellationToken.None);

        result.ShouldBeSuccess();
        var expected = shipping.CalculateCostForCart(
            Money.FromDecimal(500_000m),
            [new ShippingCostItem(variant.Id, 1m, 2)]);
        result.Value.Amount.ShouldBe(expected.Amount);
    }

    [Fact]
    public async Task ValidateAndCalculateCostAsync_WhenVariantHasMultiplier_AppliesIt()
    {
        var shipping = new ShippingBuilder()
            .WithName($"Weighted Ship {Guid.NewGuid():N}"[..21])
            .WithBaseCost(80_000m, "IRT")
            .Build();
        var variant = NewVariant();
        variant.SetShippingMethods(
            2.5m,
            [new ShippingAssignment(shipping.Id, 1m, 10m, 10m, 10m)]);
        variant.ClearDomainEvents();
        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns(shipping);
        _variantRepository
            .GetByIdsWithShippingsAsync(Arg.Any<IEnumerable<global::Domain.Variant.ValueObjects.VariantId>>(), Arg.Any<CancellationToken>())
            .Returns([variant]);

        var items = new[] { NewSnapshot(variant) };
        var result = await _sut.ValidateAndCalculateCostAsync(
            shipping.Id.Value, 500_000m, items, CancellationToken.None);

        result.ShouldBeSuccess();
        var expected = shipping.CalculateCostForCart(
            Money.FromDecimal(500_000m),
            [new ShippingCostItem(variant.Id, 2.5m, 1)]);
        result.Value.Amount.ShouldBe(expected.Amount);
    }

    [Fact]
    public async Task ValidateAndCalculateCostAsync_WhenVariantIsMissingFromRepository_FallsBackToMultiplierOne()
    {
        var shipping = new ShippingBuilder()
            .WithName($"Missing Ship {Guid.NewGuid():N}"[..20])
            .WithBaseCost(80_000m, "IRT")
            .Build();
        var variant = NewVariant();
        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns(shipping);
        _variantRepository
            .GetByIdsWithShippingsAsync(Arg.Any<IEnumerable<global::Domain.Variant.ValueObjects.VariantId>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var items = new[] { NewSnapshot(variant) };
        var result = await _sut.ValidateAndCalculateCostAsync(
            shipping.Id.Value, 500_000m, items, CancellationToken.None);

        result.ShouldBeSuccess();
        var expected = shipping.CalculateCostForCart(
            Money.FromDecimal(500_000m),
            [new ShippingCostItem(variant.Id, 1m, 1)]);
        result.Value.Amount.ShouldBe(expected.Amount);
    }
}
