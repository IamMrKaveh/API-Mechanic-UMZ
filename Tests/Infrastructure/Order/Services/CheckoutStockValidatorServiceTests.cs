using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Domain.Inventory.Aggregates;
using Domain.Inventory.Interfaces;
using Domain.Order.ValueObjects;
using Infrastructure.Order.Services;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Infrastructure.Order.Services;

public class CheckoutStockValidatorServiceTests
{
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>();
    private readonly CheckoutStockValidatorService _sut;

    public CheckoutStockValidatorServiceTests()
    {
        _sut = new CheckoutStockValidatorService(_inventoryRepository);
    }

    private static OrderItemSnapshot NewSnapshot(int quantity = 1) =>
        new OrderItemSnapshotBuilder()
            .WithQuantity(quantity)
            .Build();

    [Fact]
    public async Task ValidateAsync_WhenItemsAreEmpty_SucceedsWithoutRepositoryCall()
    {
        var result = await _sut.ValidateAsync([], CancellationToken.None);

        result.ShouldBeSuccess();
        await _inventoryRepository.DidNotReceiveWithAnyArgs().GetByVariantIdAsync(default!, default);
    }

    [Fact]
    public async Task ValidateAsync_WhenStockIsSufficient_Succeeds()
    {
        var item = NewSnapshot(quantity: 2);
        var inventory = new InventoryBuilder()
            .WithVariantId(item.VariantId)
            .WithInitialStock(10)
            .Build();
        _inventoryRepository
            .GetByVariantIdAsync(item.VariantId, Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.ValidateAsync([item], CancellationToken.None);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task ValidateAsync_WhenInventoryIsMissing_ReturnsFailure()
    {
        var item = NewSnapshot();
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<global::Domain.Variant.ValueObjects.VariantId>(), Arg.Any<CancellationToken>())
            .Returns((global::Domain.Inventory.Aggregates.Inventory?)null);

        var result = await _sut.ValidateAsync([item], CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        result.Error.Message.ShouldContain(item.VariantId.Value.ToString());
    }

    [Fact]
    public async Task ValidateAsync_WhenStockIsInsufficient_ReturnsFailureWithProductName()
    {
        var item = new OrderItemSnapshotBuilder()
            .WithQuantity(5)
            .Build();
        var inventory = new InventoryBuilder()
            .WithVariantId(item.VariantId)
            .WithInitialStock(2)
            .Build();
        _inventoryRepository
            .GetByVariantIdAsync(item.VariantId, Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.ValidateAsync([item], CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        result.Error.Message.ShouldContain(item.ProductName.Value);
    }

    [Fact]
    public async Task ValidateAsync_WhenUnlimitedStock_SucceedsForAnyQuantity()
    {
        var item = NewSnapshot(quantity: 1000);
        var inventory = new InventoryBuilder()
            .WithVariantId(item.VariantId)
            .AsUnlimited()
            .Build();
        _inventoryRepository
            .GetByVariantIdAsync(item.VariantId, Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.ValidateAsync([item], CancellationToken.None);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task ValidateAsync_WhenMultipleItemsFail_JoinsAllErrors()
    {
        var first = NewSnapshot();
        var second = NewSnapshot();
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<global::Domain.Variant.ValueObjects.VariantId>(), Arg.Any<CancellationToken>())
            .Returns((global::Domain.Inventory.Aggregates.Inventory?)null);

        var result = await _sut.ValidateAsync([first, second], CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        result.Error.Message.ShouldContain(" | ");
    }
}
