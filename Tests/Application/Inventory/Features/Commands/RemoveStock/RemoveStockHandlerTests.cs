using Application.Audit.Contracts;
using Application.Cache.Contracts;
using Application.Common.Interfaces;
using Application.Inventory.Features.Commands.RemoveStock;
using Domain.Inventory.Interfaces;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Inv = Domain.Inventory.Aggregates.Inventory;
using PV = Domain.Variant.Aggregates.ProductVariant;

namespace Tests.Application.Inventory.Features.Commands.RemoveStock;

public class RemoveStockHandlerTests
{
    private readonly IVariantRepository _variantRepository = Substitute.For<IVariantRepository>(); private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly RemoveStockHandler _sut;

    public RemoveStockHandlerTests()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _sut = new RemoveStockHandler(
            _variantRepository,
            _inventoryRepository,
            _auditService,
            _cacheService,
            _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenVariantNotFound_ReturnsNotFound()
    {
        _variantRepository
            .GetByIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((PV?)null);

        var result = await _sut.Handle(
            new RemoveStockCommand(Guid.NewGuid(), 1, "notes"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        await _inventoryRepository.DidNotReceiveWithAnyArgs().GetByVariantIdAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenInventoryNotFound_ReturnsNotFound()
    {
        var variant = new ProductVariantBuilder().Build();
        _variantRepository
            .GetByIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((Inv?)null);

        var result = await _sut.Handle(
            new RemoveStockCommand(variant.Id.Value, 1, "notes"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WithValidState_DecreasesStockAuditsAndInvalidatesCaches()
    {
        var variant = new ProductVariantBuilder().Build();
        var inventory = new InventoryBuilder().WithVariantId(variant.Id).WithInitialStock(10).Build();

        _variantRepository
            .GetByIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.Handle(
            new RemoveStockCommand(variant.Id.Value, 3, "damaged"),
            CancellationToken.None);

        result.ShouldBeSuccess();
        inventory.StockQuantity.Value.ShouldBe(7);
        _inventoryRepository.Received(1).Update(inventory);
        await _auditService.Received(1).LogInventoryEventAsync(
            inventory.VariantId,
            "RemoveStock",
            Arg.Any<string>(),
            Arg.Any<UserId?>());
        await _cacheService.Received(1).RemoveAsync($"product:{variant.ProductId.Value}", Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveAsync($"variant:{variant.Id.Value}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDecreaseExceedsStock_ReturnsFailure()
    {
        var variant = new ProductVariantBuilder().Build();
        var inventory = new InventoryBuilder().WithVariantId(variant.Id).WithInitialStock(2).Build();

        _variantRepository
            .GetByIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.Handle(
            new RemoveStockCommand(variant.Id.Value, 5, "notes"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        _inventoryRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }
}
