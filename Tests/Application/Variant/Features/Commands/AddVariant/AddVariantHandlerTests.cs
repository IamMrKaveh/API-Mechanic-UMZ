using Application.Audit.Contracts;
using Application.Common.Interfaces;
using Application.Variant.Features.Commands.AddVariant;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Inventory.Interfaces;
using Domain.Product.Interfaces;
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
using Products = Domain.Product.Aggregates.Product;
using Shippings = Domain.Shipping.Aggregates.Shipping;

namespace Tests.Application.Variant.Features.Commands.AddVariant;

public class AddVariantHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>(); private readonly IVariantRepository _variantRepository = Substitute.For<IVariantRepository>(); private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>(); private readonly IAttributeRepository _attributeRepository = Substitute.For<IAttributeRepository>(); private readonly IShippingRepository _shippingRepository = Substitute.For<IShippingRepository>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly ILogger<AddVariantHandler> _logger = Substitute.For<ILogger<AddVariantHandler>>(); private readonly AddVariantHandler _sut;

    public AddVariantHandlerTests()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());

        _variantRepository
            .ExistsBySkuAsync(Arg.Any<Sku>(), Arg.Any<VariantId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _variantRepository
            .ExistsByAttributeCombinationAsync(
                Arg.Any<ProductId>(),
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<VariantId?>(),
                Arg.Any<CancellationToken>())
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

        _sut = new AddVariantHandler(
            _productRepository,
            _variantRepository,
            _inventoryRepository,
            _attributeRepository,
            _shippingRepository,
            _unitOfWork,
            _auditService,
            _currentUserService,
            _logger);
    }

    private static Products BuildProduct()
    {
        return new ProductBuilder()
            .WithName(ProductName.Create("Test Product"))
            .WithSlug(ProductSlug.Create("test-product"))
            .WithDescription("desc")
            .WithBrandId(BrandId.NewId())
            .WithCategoryId(CategoryId.NewId())
            .Build();
    }

    private void ConfigureExistingProduct(Products? product = null)
    {
        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(product ?? BuildProduct());
    }

    private static AddVariantCommand ValidCommand(
        Guid? productId = null,
        string? sku = null,
        decimal sellingPrice = 100_000m,
        decimal originalPrice = 0m,
        int stock = 10,
        bool isUnlimited = false,
        decimal shippingMultiplier = 1m,
        ICollection<Guid>? attributeValueIds = null,
        ICollection<Guid>? enabledShippingIds = null)
    {
        return new AddVariantCommand(
            productId ?? Guid.NewGuid(),
            sku,
            sellingPrice,
            originalPrice,
            stock,
            isUnlimited,
            shippingMultiplier,
            attributeValueIds ?? Array.Empty<Guid>(),
            enabledShippingIds ?? Array.Empty<Guid>());
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _productRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default!, default);
        await _variantRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ReturnsNotFound()
    {
        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns((Products?)null);

        var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        await _variantRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _inventoryRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenAttributeValueIdsMissingFromRepository_ReturnsValidation()
    {
        ConfigureExistingProduct();
        var attributeIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        _attributeRepository
            .GetAttributeValuesByIdsAsync(Arg.Any<IEnumerable<AttributeValueId>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Attributes>());

        var result = await _sut.Handle(
            ValidCommand(attributeValueIds: attributeIds),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        await _variantRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenSkuAlreadyExists_ReturnsConflict()
    {
        ConfigureExistingProduct();
        _variantRepository
            .ExistsBySkuAsync(Arg.Any<Sku>(), Arg.Any<VariantId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(
            ValidCommand(sku: "ABC-123"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _variantRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenSkuHasInvalidFormat_ReturnsValidation()
    {
        ConfigureExistingProduct();

        var result = await _sut.Handle(
            ValidCommand(sku: "invalid sku!!"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        await _variantRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenOriginalPricePositiveAndLessThanSellingPrice_ReturnsValidation()
    {
        ConfigureExistingProduct();

        var result = await _sut.Handle(
            ValidCommand(sellingPrice: 100_000m, originalPrice: 50_000m),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        await _variantRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenShippingIdsMissingFromRepository_ReturnsValidation()
    {
        ConfigureExistingProduct();
        _shippingRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<ShippingId>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Shippings>());

        var result = await _sut.Handle(
            ValidCommand(enabledShippingIds: new[] { Guid.NewGuid() }),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        await _variantRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WithValidCommandAndNoAttributesOrShipping_ReturnsSuccessAndPersists()
    {
        ConfigureExistingProduct();

        var command = ValidCommand(sku: "SKU-001", sellingPrice: 100_000m, stock: 5);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Sku.ShouldBe("SKU-001");
        result.Value.SellingPrice.ShouldBe(100_000m);
        await _variantRepository.Received(1).AddAsync(Arg.Any<ProductVariant>(), Arg.Any<CancellationToken>());
        await _inventoryRepository.Received(1).AddAsync(Arg.Any<Inventories>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogProductEventAsync(
            Arg.Any<ProductId>(),
            "AddVariant",
            Arg.Any<string>(),
            Arg.Any<UserId>());
    }

    [Fact]
    public async Task Handle_WithUnlimitedStock_CreatesInventoryAsUnlimited()
    {
        ConfigureExistingProduct();

        var command = ValidCommand(sku: "SKU-UNL", stock: 0, isUnlimited: true);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.IsUnlimited.ShouldBeTrue();
        await _inventoryRepository.Received(1).AddAsync(
            Arg.Is<Inventories>(i => i!.IsUnlimited),
            Arg.Any<CancellationToken>());
    }
}
