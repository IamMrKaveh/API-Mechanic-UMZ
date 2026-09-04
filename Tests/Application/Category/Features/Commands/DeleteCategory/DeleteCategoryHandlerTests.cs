using Application.Cache.Contracts;
using Application.Category.Features.Commands.DeleteCategory;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Stubs;
using Categories = Domain.Category.Aggregates.Category;

namespace Tests.Application.Category.Features.Commands.DeleteCategory;

public class DeleteCategoryHandlerTests
{
    private readonly ICategoryRepository _repository = Substitute.For<ICategoryRepository>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly DeleteCategoryHandler _sut;

    public DeleteCategoryHandlerTests()
    {
        _sut = new DeleteCategoryHandler(_repository, _cacheService);
    }

    private static Task<Categories> BuildCategoryAsync() =>
        new CategoryBuilder()
            .WithUniquenessChecker(new StubCategoryUniquenessChecker())
            .BuildAsync();

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ReturnsNotFound()
    {
        _repository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns((Categories?)null);

        var result = await _sut.Handle(
            new DeleteCategoryCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
        _repository.DidNotReceive().Update(Arg.Any<Categories>(), Arg.Any<byte[]?>());
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveByPrefixAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenCategoryHasBrands_ReturnsFailure()
    {
        var category = await BuildCategoryAsync();
        _repository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(category);
        _repository
            .HasBrandAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(
            new DeleteCategoryCommand(category.Id.Value),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Failure);
        _repository.DidNotReceive().Update(Arg.Any<Categories>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenCategoryHasNoBrands_DeactivatesCategoryAndReturnsSuccess()
    {
        var category = await BuildCategoryAsync();
        _repository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(category);
        _repository
            .HasBrandAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.Handle(
            new DeleteCategoryCommand(category.Id.Value),
            CancellationToken.None);

        result.ShouldBeSuccess();
        category.IsActive.ShouldBeFalse();
        await _cacheService.Received(1).RemoveByPrefixAsync("categories:", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCategoryHasNoBrands_CallsUpdateOnceOnRepository()
    {
        var category = await BuildCategoryAsync();
        _repository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(category);
        _repository
            .HasBrandAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await _sut.Handle(new DeleteCategoryCommand(category.Id.Value), CancellationToken.None);

        _repository.Received(1).Update(
            Arg.Is<Categories>(c => c!.Id == category.Id),
            Arg.Any<byte[]?>());
    }
}
