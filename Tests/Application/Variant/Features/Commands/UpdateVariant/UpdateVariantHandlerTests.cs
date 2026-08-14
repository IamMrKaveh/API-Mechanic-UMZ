using Application.Audit.Contracts;
using Application.Common.Interfaces;
using Application.Variant.Features.Commands.UpdateVariant;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using Domain.Inventory.Interfaces;
using Domain.Product.ValueObjects;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Attributes = Domain.Attribute.Entities.AttributeValue;
using Inventories = Domain.Inventory.Aggregates.Inventory;
using Shippings = Domain.Shipping.Aggregates.Shipping;

namespace Tests.Application.Variant.Features.Commands.UpdateVariant;

public class UpdateVariantHandlerTests
{
    private readonly IVariantRepository _variantRepository = Substitute.For<IVariantRepository>(); private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>(); private readonly IAttributeRepository _attributeRepository = Substitute.For<IAttributeRepository>(); private readonly IShippingRepository _shippingRepository = Substitute.For<IShippingRepository>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly UpdateVariantHandler _sut;

    public UpdateVariantHandlerTests()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());

        _variantRepository
            .ExistsBySkuAsync(Arg.Any<Sku>(), Arg.Any<VariantId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _shippingRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<ShippingId>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Shippings>());

        _attributeRepository
            .GetAttributeValuesByIdsAsync(Arg.Any<IEnumerable<AttributeValueId>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Attributes>());

        _unitOfWork
            .ExecuteStrategyAsync(Arg.Any<Func<CancellationToken, Task<bool>>>(), Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                var op = ci.Arg<Func<CancellationToken, Task<bool>>>();
                return await op!(ci.Arg<CancellationToken>());
            });

        _sut = new UpdateVariantHandler(
            _variantRepository,
            _inventoryRepository,
            _attributeRepository,
            _shippingRepository,
            _unitOfWork,
            _auditService,
            _currentUserService);
    }

    private static ProductVariant BuildVariant(ProductId productId, VariantId? variantId = null, string sku = "OLD-SKU")
    {
        return new ProductVariantBuilder()
            .WithId(variantId ?? VariantId.NewId())
            .WithProductId(productId)
            .WithSku(sku)
            .WithSellingPrice(100_000m)
            .WithOriginalPrice(120_000m)
            .Build();
    }

    private static UpdateVariantCommand ValidCommand(
        Guid productId,
        Guid variantId,
        string? sku = null,
        decimal sellingPrice = 100_000m,
        decimal originalPrice = 0m,
        int stock = 5,
        bool isUnlimited = false,
        decimal shippingMultiplier = 1m,
        ICollection<Guid>? attributeValueIds = null,
        ICollection<Guid>? enabledShippingIds = null)
    {
        return new UpdateVariantCommand(
            productId,
            variantId,
            sku,
            sellingPrice,
            originalPrice,
            stock,
            isUnlimited,
            shippingMultiplier,
            attributeValueIds,
            enabledShippingIds);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var command = ValidCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _variantRepository.DidNotReceiveWithAnyArgs().GetForUpdateAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenVariantNotFound_ReturnsNotFound()
    {
        _variantRepository
            .GetForUpdateAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((ProductVariant?)null);

        var command = ValidCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenVariantBelongsToDifferentProduct_ReturnsValidation()
    {
        var variantProductId = ProductId.NewId();
        var variantId = VariantId.NewId();
        var variant = BuildVariant(variantProductId, variantId);

        _variantRepository
            .GetForUpdateAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);

        var command = ValidCommand(Guid.NewGuid(), variantId.Value);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
    }

    [Fact]
    public async Task Handle_WhenNewSkuAlreadyExists_ReturnsConflict()
    {
        var productId = ProductId.NewId();
        var variantId = VariantId.NewId();
        var variant = BuildVariant(productId, variantId, "OLD-SKU");

        _variantRepository
            .GetForUpdateAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _variantRepository
            .ExistsBySkuAsync(Arg.Any<Sku>(), Arg.Any<VariantId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = ValidCommand(productId.Value, variantId.Value, sku: "NEW-SKU");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
    }

    [Fact]
    public async Task Handle_WhenAttributeValueIdsMissingFromRepository_ReturnsValidation()
    {
        var productId = ProductId.NewId();
        var variantId = VariantId.NewId();
        var variant = BuildVariant(productId, variantId);

        _variantRepository
            .GetForUpdateAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _attributeRepository
            .GetAttributeValuesByIdsAsync(Arg.Any<IEnumerable<AttributeValueId>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var command = ValidCommand(
            productId.Value,
            variantId.Value,
            attributeValueIds: new[] { Guid.NewGuid() });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
    }

    [Fact]
    public async Task Handle_WhenEnabledShippingIdsMissingFromRepository_ReturnsValidation()
    {
        var productId = ProductId.NewId();
        var variantId = VariantId.NewId();
        var variant = BuildVariant(productId, variantId);

        _variantRepository
            .GetForUpdateAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _shippingRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<ShippingId>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Shippings>());

        var command = ValidCommand(
            productId.Value,
            variantId.Value,
            enabledShippingIds: new[] { Guid.NewGuid() });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
    }

    [Fact]
    public async Task Handle_WithExistingInventoryAndValidUpdate_ReturnsSuccessAndSaves()
    {
        var productId = ProductId.NewId();
        var variantId = VariantId.NewId();
        var variant = BuildVariant(productId, variantId);
        var inventory = new InventoryBuilder()
            .WithVariantId(variantId)
            .WithInitialStock(10)
            .Build();

        _variantRepository
            .GetForUpdateAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var command = ValidCommand(
            productId.Value,
            variantId.Value,
            sellingPrice: 200_000m,
            originalPrice: 250_000m,
            stock: 15,
            isUnlimited: false);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogProductEventAsync(
            Arg.Any<ProductId>(),
            "UpdateVariant",
            Arg.Any<string>(),
            Arg.Any<UserId>());
    }

    [Fact]
    public async Task Handle_WhenInventoryDoesNotExist_CreatesInventory()
    {
        var productId = ProductId.NewId();
        var variantId = VariantId.NewId();
        var variant = BuildVariant(productId, variantId);

        _variantRepository
            .GetForUpdateAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((Inventories?)null);

        var command = ValidCommand(
            productId.Value,
            variantId.Value,
            sellingPrice: 200_000m,
            stock: 20);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        await _inventoryRepository.Received(1).AddAsync(Arg.Any<Inventories>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUnlimitedRequestedOnExistingInventory_SetsUnlimited()
    {
        var productId = ProductId.NewId();
        var variantId = VariantId.NewId();
        var variant = BuildVariant(productId, variantId);
        var inventory = new InventoryBuilder()
            .WithVariantId(variantId)
            .WithInitialStock(5)
            .Build();

        _variantRepository
            .GetForUpdateAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var command = ValidCommand(
            productId.Value,
            variantId.Value,
            sellingPrice: 200_000m,
            stock: 0,
            isUnlimited: true);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        inventory.IsUnlimited.ShouldBeTrue();
    }
}
