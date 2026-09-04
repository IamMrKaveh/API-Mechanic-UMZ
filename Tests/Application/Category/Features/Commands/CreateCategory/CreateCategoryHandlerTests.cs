using Application.Cache.Contracts;
using Application.Category.Features.Commands.CreateCategory;
using Domain.Category.Exceptions;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Mapping;
using Categories = Domain.Category.Aggregates.Category;

namespace Tests.Application.Category.Features.Commands.CreateCategory;

public class CreateCategoryHandlerTests : IClassFixture<MapsterConfigFixture>
{
    private readonly ICategoryRepository _repository = Substitute.For<ICategoryRepository>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly CreateCategoryHandler _sut;

    public CreateCategoryHandlerTests(MapsterConfigFixture _)
    {
        _repository
            .ExistsByNameAsync(Arg.Any<CategoryName>(), Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _repository
            .ExistsBySlugAsync(Arg.Any<CategorySlug>(), Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _sut = new CreateCategoryHandler(_repository, _cacheService);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccessWithMappedDto()
    {
        var command = new CreateCategoryCommand("Books", null, "Books description", 5);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Name.ShouldBe("Books");
        result.Value.Slug.ShouldBe("books");
        result.Value.Description.ShouldBe("Books description");
        result.Value.IsActive.ShouldBeTrue();
        result.Value.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CallsAddAsyncOnRepositoryOnce()
    {
        var command = new CreateCategoryCommand("Movies", "movies", null, 0);

        await _sut.Handle(command, CancellationToken.None);

        await _repository.Received(1)
            .AddAsync(Arg.Any<Categories>(), Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveByPrefixAsync("categories:", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithProvidedSlug_UsesSlugFromCommand()
    {
        var command = new CreateCategoryCommand("Cars", "my-cars", null, 0);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Slug.ShouldBe("my-cars");
    }

    [Fact]
    public async Task Handle_WithWhitespaceSlug_GeneratesSlugFromCategoryName()
    {
        var command = new CreateCategoryCommand("Home Appliances", "   ", null, 0);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Slug.ShouldBe("home-appliances");
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ThrowsDuplicateCategoryNameException()
    {
        _repository
            .ExistsByNameAsync(Arg.Any<CategoryName>(), Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var command = new CreateCategoryCommand("Books", null, null, 0);

        await Should.ThrowAsync<DuplicateCategoryNameException>(
            () => _sut.Handle(command, CancellationToken.None));

        await _repository.DidNotReceive()
            .AddAsync(Arg.Any<Categories>(), Arg.Any<CancellationToken>());
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveByPrefixAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenSlugAlreadyExists_ThrowsDuplicateCategoryNameException()
    {
        _repository
            .ExistsBySlugAsync(Arg.Any<CategorySlug>(), Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var command = new CreateCategoryCommand("Books", "books", null, 0);

        await Should.ThrowAsync<DuplicateCategoryNameException>(
            () => _sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithInvalidCategoryName_ThrowsDomainException()
    {
        var command = new CreateCategoryCommand("", null, null, 0);

        await Should.ThrowAsync<DomainException>(
            () => _sut.Handle(command, CancellationToken.None));
    }
}
