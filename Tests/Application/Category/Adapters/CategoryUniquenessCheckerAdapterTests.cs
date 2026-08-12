using Application.Category.Adapters;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;

namespace Tests.Application.Category.Adapters;

public class CategoryUniquenessCheckerAdapterTests
{
    private readonly ICategoryRepository _repository = Substitute.For<ICategoryRepository>(); private readonly CategoryUniquenessCheckerAdapter _sut;

    public CategoryUniquenessCheckerAdapterTests()
    {
        _sut = new CategoryUniquenessCheckerAdapter(_repository);
    }

    [Fact]
    public async Task IsUniqueAsync_WhenNameExists_ReturnsFalseWithoutCheckingSlug()
    {
        _repository
            .ExistsByNameAsync(Arg.Any<CategoryName>(), Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.IsUniqueAsync(
            CategoryName.Create("Books"),
            CategorySlug.Create("books"),
            null,
            CancellationToken.None);

        result.ShouldBeFalse();
        await _repository.DidNotReceive().ExistsBySlugAsync(
            Arg.Any<CategorySlug>(), Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IsUniqueAsync_WhenNameDoesNotExistAndSlugExists_ReturnsFalse()
    {
        _repository
            .ExistsByNameAsync(Arg.Any<CategoryName>(), Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _repository
            .ExistsBySlugAsync(Arg.Any<CategorySlug>(), Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.IsUniqueAsync(
            CategoryName.Create("Books"),
            CategorySlug.Create("books"),
            null,
            CancellationToken.None);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsUniqueAsync_WhenNeitherNameNorSlugExists_ReturnsTrue()
    {
        _repository
            .ExistsByNameAsync(Arg.Any<CategoryName>(), Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _repository
            .ExistsBySlugAsync(Arg.Any<CategorySlug>(), Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.IsUniqueAsync(
            CategoryName.Create("Books"),
            CategorySlug.Create("books"),
            null,
            CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsUniqueAsync_ForwardsExcludeIdToRepositoryChecks()
    {
        var excludeId = CategoryId.NewId();
        _repository
            .ExistsByNameAsync(Arg.Any<CategoryName>(), Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _repository
            .ExistsBySlugAsync(Arg.Any<CategorySlug>(), Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await _sut.IsUniqueAsync(
            CategoryName.Create("Books"),
            CategorySlug.Create("books"),
            excludeId,
            CancellationToken.None);

        await _repository.Received(1).ExistsByNameAsync(
            Arg.Any<CategoryName>(),
            Arg.Is<CategoryId?>(x => x != null && x.Value == excludeId.Value),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).ExistsBySlugAsync(
            Arg.Any<CategorySlug>(),
            Arg.Is<CategoryId?>(x => x != null && x.Value == excludeId.Value),
            Arg.Any<CancellationToken>());
    }
}
