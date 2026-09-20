using Application.Brand.Adapters;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Tests.Application.Brand.Adapters;

public class BrandUniquenessCheckerAdapterTests
{
    private readonly IBrandRepository _repository = Substitute.For<IBrandRepository>();
    private readonly BrandUniquenessCheckerAdapter _sut;

    public BrandUniquenessCheckerAdapterTests()
    {
        _sut = new BrandUniquenessCheckerAdapter(_repository);
    }

    [Fact]
    public async Task IsUniqueAsync_WhenNeitherNameNorSlugExists_ReturnsTrue()
    {
        _repository.ExistsByNameInCategoryAsync(Arg.Any<BrandName>(), Arg.Any<CategoryId>(), Arg.Any<BrandId?>(), Arg.Any<CancellationToken>()).Returns(false);
        _repository.ExistsBySlugAsync(Arg.Any<BrandSlug>(), Arg.Any<BrandId?>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.IsUniqueAsync(BrandName.Create("Nike"), BrandSlug.Create("nike"), CategoryId.NewId(), null, CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsUniqueAsync_WhenNameExists_ReturnsFalseWithoutCheckingSlug()
    {
        _repository.ExistsByNameInCategoryAsync(Arg.Any<BrandName>(), Arg.Any<CategoryId>(), Arg.Any<BrandId?>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.IsUniqueAsync(BrandName.Create("Nike"), BrandSlug.Create("nike"), CategoryId.NewId(), null, CancellationToken.None);

        result.ShouldBeFalse();
        await _repository.DidNotReceive().ExistsBySlugAsync(Arg.Any<BrandSlug>(), Arg.Any<BrandId?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IsUniqueAsync_WhenSlugExists_ReturnsFalse()
    {
        _repository.ExistsByNameInCategoryAsync(Arg.Any<BrandName>(), Arg.Any<CategoryId>(), Arg.Any<BrandId?>(), Arg.Any<CancellationToken>()).Returns(false);
        _repository.ExistsBySlugAsync(Arg.Any<BrandSlug>(), Arg.Any<BrandId?>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.IsUniqueAsync(BrandName.Create("Nike"), BrandSlug.Create("nike"), CategoryId.NewId(), null, CancellationToken.None);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsUniqueAsync_ForwardsExcludeIdToBothChecks()
    {
        var excludeId = BrandId.NewId();
        var categoryId = CategoryId.NewId();
        var name = BrandName.Create("Adidas");
        var slug = BrandSlug.Create("adidas");
        BrandId? capturedNameExclude = null;
        BrandId? capturedSlugExclude = null;
        _repository.ExistsByNameInCategoryAsync(Arg.Any<BrandName>(), Arg.Any<CategoryId>(), Arg.Do<BrandId?>(x => capturedNameExclude = x), Arg.Any<CancellationToken>()).Returns(false);
        _repository.ExistsBySlugAsync(Arg.Any<BrandSlug>(), Arg.Do<BrandId?>(x => capturedSlugExclude = x), Arg.Any<CancellationToken>()).Returns(false);

        await _sut.IsUniqueAsync(name, slug, categoryId, excludeId, CancellationToken.None);

        capturedNameExclude.ShouldBe(excludeId);
        capturedSlugExclude.ShouldBe(excludeId);
    }

    [Fact]
    public async Task IsUniqueAsync_ForwardsNameSlugCategoryAndCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var categoryId = CategoryId.NewId();
        var name = BrandName.Create("Puma");
        var slug = BrandSlug.Create("puma");
        BrandName? capturedName = null;
        BrandSlug? capturedSlug = null;
        CategoryId? capturedCategory = null;
        CancellationToken capturedCt = default;
        _repository.ExistsByNameInCategoryAsync(
                Arg.Do<BrandName>(x => capturedName = x),
                Arg.Do<CategoryId>(x => capturedCategory = x),
                Arg.Any<BrandId?>(),
                Arg.Do<CancellationToken>(x => capturedCt = x))
            .Returns(false);
        _repository.ExistsBySlugAsync(Arg.Do<BrandSlug>(x => capturedSlug = x), Arg.Any<BrandId?>(), Arg.Any<CancellationToken>()).Returns(false);

        await _sut.IsUniqueAsync(name, slug, categoryId, null, cts.Token);

        capturedName.ShouldBe(name);
        capturedSlug.ShouldBe(slug);
        capturedCategory.ShouldBe(categoryId);
        capturedCt.ShouldBe(cts.Token);
    }
}
