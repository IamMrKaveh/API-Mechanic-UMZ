using Domain.Variant.Aggregates;
using Domain.Variant.Interfaces;

namespace Tests.ApplicationTest.Product;

public class ChangePriceHandlerTests
{
    private readonly IVariantRepository _variantRepository = Substitute.For<IVariantRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
    private readonly ChangePriceHandler _handler;

    public ChangePriceHandlerTests()
    {
        _currentUserService.UserId.Returns(1);
        _handler = new ChangePriceHandler(
            _variantRepository, _unitOfWork, _auditService, _currentUserService, _cacheService);
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Variant_Not_Found()
    {
        _variantRepository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((ProductVariant?)null);

        var command = new ChangePriceCommand
        {
            ProductId = 1,
            VariantId = 999,
            PurchasePrice = 50_000,
            SellingPrice = 100_000,
            OriginalPrice = 120_000
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Variant_Belongs_To_Different_Product()
    {
        var variant = new ProductVariantBuilder().WithProductId(5).Build();

        _variantRepository.GetByIdAsync(variant.Id, Arg.Any<CancellationToken>())
            .Returns(variant);

        var command = new ChangePriceCommand
        {
            ProductId = 99,
            VariantId = variant.Id,
            PurchasePrice = 50_000,
            SellingPrice = 100_000,
            OriginalPrice = 120_000
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_Should_Update_Pricing_And_Save()
    {
        var variant = new ProductVariantBuilder().WithProductId(1).Build();

        _variantRepository.GetByIdAsync(variant.Id, Arg.Any<CancellationToken>())
            .Returns(variant);

        var command = new ChangePriceCommand
        {
            ProductId = 1,
            VariantId = variant.Id,
            PurchasePrice = 60_000,
            SellingPrice = 110_000,
            OriginalPrice = 130_000
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(110_000, variant.SellingPrice.Amount);
        _variantRepository.Received(1).Update(variant);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Domain_Validation_Fails()
    {
        var variant = new ProductVariantBuilder().WithProductId(1).Build();

        _variantRepository.GetByIdAsync(variant.Id, Arg.Any<CancellationToken>())
            .Returns(variant);

        var command = new ChangePriceCommand
        {
            ProductId = 1,
            VariantId = variant.Id,
            PurchasePrice = 100_000,
            SellingPrice = 50_000,
            OriginalPrice = 120_000
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_Should_Clear_Cache_On_Success()
    {
        var variant = new ProductVariantBuilder().WithProductId(1).Build();

        _variantRepository.GetByIdAsync(variant.Id, Arg.Any<CancellationToken>())
            .Returns(variant);

        var command = new ChangePriceCommand
        {
            ProductId = 1,
            VariantId = variant.Id,
            PurchasePrice = 60_000,
            SellingPrice = 110_000,
            OriginalPrice = 130_000
        };

        await _handler.Handle(command, CancellationToken.None);

        await _cacheService.Received().ClearAsync($"product:{1}");
        await _cacheService.Received().ClearAsync($"variant:{variant.Id}");
    }
}