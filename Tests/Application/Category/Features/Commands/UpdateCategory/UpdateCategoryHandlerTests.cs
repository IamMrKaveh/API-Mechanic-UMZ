using Application.Category.Features.Commands.UpdateCategory;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Mapping;
using Tests.TestInfrastructure.Stubs;
using Categories = Domain.Category.Aggregates.Category;

namespace Tests.Application.Category.Features.Commands.UpdateCategory;

public class UpdateCategoryHandlerTests : IClassFixture<MapsterConfigFixture>
{
    private readonly ICategoryRepository _repository = Substitute.For<ICategoryRepository>(); private readonly UpdateCategoryHandler _sut;

    public UpdateCategoryHandlerTests(MapsterConfigFixture _)
    {
        _repository
            .ExistsByNameAsync(Arg.Any<CategoryName>(), Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _repository
            .ExistsBySlugAsync(Arg.Any<CategorySlug>(), Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _sut = new UpdateCategoryHandler(_repository);
    }

    private async Task<Categories> BuildActiveCategoryAsync() =>
        await new CategoryBuilder()
            .WithName(CategoryName.Create("Original"))
            .WithSlug(CategorySlug.Create("original"))
            .WithUniquenessChecker(new StubCategoryUniquenessChecker())
            .BuildAsync();

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ReturnsNotFound()
    {
        _repository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns((Categories?)null);

        var command = new UpdateCategoryCommand(
            Guid.NewGuid(), "New", true, null, null, 0, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccessWithUpdatedDto()
    {
        var category = await BuildActiveCategoryAsync();
        _repository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(category);

        var command = new UpdateCategoryCommand(
            category.Id.Value, "Renamed", true, "renamed", "new desc", 7, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Name.ShouldBe("Renamed");
        result.Value.Slug.ShouldBe("renamed");
        result.Value.Description.ShouldBe("new desc");
    }

    [Fact]
    public async Task Handle_WithNullSlug_GeneratesSlugFromName()
    {
        var category = await BuildActiveCategoryAsync();
        _repository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(category);

        var command = new UpdateCategoryCommand(
            category.Id.Value, "Home Appliances", true, null, null, 0, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Slug.ShouldBe("home-appliances");
    }

    [Fact]
    public async Task Handle_WhenIsActiveFalseAndCategoryActive_DeactivatesCategory()
    {
        var category = await BuildActiveCategoryAsync();
        _repository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(category);

        var command = new UpdateCategoryCommand(
            category.Id.Value, "Original", false, "original", null, 0, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        category.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_WhenIsActiveTrueAndCategoryInactive_ActivatesCategory()
    {
        var category = await BuildActiveCategoryAsync();
        category.Deactivate();
        _repository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(category);

        var command = new UpdateCategoryCommand(
            category.Id.Value, "Original", true, "original", null, 0, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        category.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithValidBase64RowVersion_PassesDecodedRowVersionToUpdate()
    {
        var category = await BuildActiveCategoryAsync();
        _repository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(category);

        var expected = new byte[] { 1, 2, 3, 4 };
        var rowVersion = Convert.ToBase64String(expected);
        var command = new UpdateCategoryCommand(
            category.Id.Value, "Original", true, "original", null, 0, rowVersion);

        await _sut.Handle(command, CancellationToken.None);

        _repository.Received(1).Update(
            Arg.Is<Categories>(c => c!.Id == category.Id),
            Arg.Is<byte[]>(rv => rv != null && rv.SequenceEqual(expected)));
    }

    [Fact]
    public async Task Handle_WithNullRowVersion_PassesNullRowVersionToUpdate()
    {
        var category = await BuildActiveCategoryAsync();
        _repository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(category);

        var command = new UpdateCategoryCommand(
            category.Id.Value, "Original", true, "original", null, 0, null);

        await _sut.Handle(command, CancellationToken.None);

        _repository.Received(1).Update(
            Arg.Is<Categories>(c => c!.Id == category.Id),
            Arg.Is<byte[]?>(rv => rv == null));
    }
}
