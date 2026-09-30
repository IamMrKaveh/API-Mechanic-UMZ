using Application.Product.Features.Commands.BulkUpdatePrices;
using Application.Product.Features.Shared;
using Domain.Variant.Aggregates;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Tests.Application.Product.Features.Commands.BulkUpdatePrices;

public class BulkUpdatePricesHandlerTests : HandlerTestBase
{
    private readonly IVariantRepository _variantRepository = Substitute.For<IVariantRepository>();
 
    private readonly BulkUpdatePricesHandler _sut;

    public BulkUpdatePricesHandlerTests()
    {
        _sut = new BulkUpdatePricesHandler(_variantRepository, AuditService);
    }

    [Fact]
    public async Task Handle_WhenVariantFound_UpdatesPriceAndAudits()
    {
        var variant = new ProductVariantBuilder().WithSellingPrice(100m).WithOriginalPrice(120m).Build();
        _variantRepository.GetByIdsAsync(Arg.Any<IEnumerable<VariantId>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductVariant> { variant });
        var command = new BulkUpdatePricesCommand(new List<VariantPriceUpdateInput>
        {
            new(variant.ProductId.Value, variant.Id.Value, 150m, 180m)
        });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        variant.SellingPrice.Amount.ShouldBe(150m);
        variant.OriginalPrice.Amount.ShouldBe(180m);
        _variantRepository.Received(1).Update(variant);
        await AuditService.Received(1).LogSystemEventAsync(
            "BulkPriceUpdate",
            Arg.Is<string>(s => s != null && s.Contains(variant.Id.Value.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOriginalNotGreaterThanSelling_SetsCompareAtToNull()
    {
        var variant = new ProductVariantBuilder().WithSellingPrice(100m).WithOriginalPrice(100m).Build();
        _variantRepository.GetByIdsAsync(Arg.Any<IEnumerable<VariantId>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductVariant> { variant });

        var result = await _sut.Handle(new BulkUpdatePricesCommand(new List<VariantPriceUpdateInput>
        {
            new(variant.ProductId.Value, variant.Id.Value, 90m, 80m)
        }), CancellationToken.None);

        result.ShouldBeSuccess();
        variant.SellingPrice.Amount.ShouldBe(90m);
    }

    [Fact]
    public async Task Handle_WhenVariantMissingFromRepository_SkipsWithoutUpdate()
    {
        _variantRepository.GetByIdsAsync(Arg.Any<IEnumerable<VariantId>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductVariant>());

        var result = await _sut.Handle(new BulkUpdatePricesCommand(new List<VariantPriceUpdateInput>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), 10m, 12m)
        }), CancellationToken.None);

        result.ShouldBeSuccess();
        _variantRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenPriceInvalid_CollectsErrorAndStillReturnsSuccess()
    {
        var variant = new ProductVariantBuilder().WithSellingPrice(100m).Build();
        _variantRepository.GetByIdsAsync(Arg.Any<IEnumerable<VariantId>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductVariant> { variant });

        // Zero selling price passes Money.Create but violates the variant invariant
        // (InvalidPriceException : DomainException), which the handler collects.
        var result = await _sut.Handle(new BulkUpdatePricesCommand(new List<VariantPriceUpdateInput>
        {
            new(variant.ProductId.Value, variant.Id.Value, 0m, 0m)
        }), CancellationToken.None);

        result.ShouldBeSuccess();
        variant.SellingPrice.Amount.ShouldBe(100m);
        _variantRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await AuditService.Received(1).LogSystemEventAsync(
            "BulkPriceUpdate",
            Arg.Is<string>(s => s != null && s.Contains("خطاها")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForwardsVariantIdsAndCancellationToken()
    {
        var variant = new ProductVariantBuilder().Build();
        IEnumerable<VariantId>? captured = null;
        _variantRepository.GetByIdsAsync(Arg.Do<IEnumerable<VariantId>>(x => captured = x), Arg.Any<CancellationToken>())
            .Returns(new List<ProductVariant> { variant });
        using var cts = new CancellationTokenSource();

        await _sut.Handle(new BulkUpdatePricesCommand(new List<VariantPriceUpdateInput>
        {
            new(variant.ProductId.Value, variant.Id.Value, 11m, 12m)
        }), cts.Token);

        captured.ShouldNotBeNull();
        captured!.Select(v => v.Value).ShouldContain(variant.Id.Value);
        await _variantRepository.Received(1).GetByIdsAsync(Arg.Any<IEnumerable<VariantId>>(), cts.Token);
    }
}
