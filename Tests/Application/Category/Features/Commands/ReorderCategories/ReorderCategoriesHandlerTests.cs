using Application.Audit.Contracts;
using Application.Category.Features.Commands.ReorderCategories;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Stubs;
using Categories = Domain.Category.Aggregates.Category;

namespace Tests.Application.Category.Features.Commands.ReorderCategories;

public class ReorderCategoriesHandlerTests
{
    private readonly ICategoryRepository _repository = Substitute.For<ICategoryRepository>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly ReorderCategoriesHandler _sut;

    public ReorderCategoriesHandlerTests()
    {
        _repository
            .ExistsByNameAsync(Arg.Any<CategoryName>(), Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _repository
            .ExistsBySlugAsync(Arg.Any<CategorySlug>(), Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _sut = new ReorderCategoriesHandler(_repository, _auditService);
    }

    private static Task<Categories> BuildCategoryAsync() =>
        new CategoryBuilder()
            .WithUniquenessChecker(new StubCategoryUniquenessChecker())
            .BuildAsync();

    [Fact]
    public async Task Handle_WithEmptyItems_ReturnsSuccessAndLogsAudit()
    {
        var command = new ReorderCategoriesCommand(new List<(Guid Id, int SortOrder)>());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        await _auditService.Received(1).LogAsync(
            "Category",
            "ReorderCategories",
            Arg.Any<IpAddress>(),
            Arg.Any<UserId?>(),
            "Category",
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCategoryExists_UpdatesSortOrderAndCallsRepositoryUpdate()
    {
        var category = await BuildCategoryAsync();
        _repository
            .GetByIdAsync(Arg.Is<CategoryId>(x => x == category.Id), Arg.Any<CancellationToken>())
            .Returns(category);

        var command = new ReorderCategoriesCommand(new List<(Guid Id, int SortOrder)>
    {
        (category.Id.Value, 42)
    });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        category.SortOrder.ShouldBe(42);
        _repository.Received(1).Update(
            Arg.Is<Categories>(c => c!.Id == category.Id),
            Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenCategoryNotFound_SkipsUpdateAndContinues()
    {
        _repository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns((Categories?)null);

        var command = new ReorderCategoriesCommand(new List<(Guid Id, int SortOrder)>
    {
        (Guid.NewGuid(), 1)
    });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        _repository.DidNotReceive().Update(Arg.Any<Categories>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WithMultipleItems_UpdatesEachExistingCategory()
    {
        var c1 = await BuildCategoryAsync();
        var c2 = await BuildCategoryAsync();
        _repository
            .GetByIdAsync(Arg.Is<CategoryId>(x => x == c1.Id), Arg.Any<CancellationToken>())
            .Returns(c1);
        _repository
            .GetByIdAsync(Arg.Is<CategoryId>(x => x == c2.Id), Arg.Any<CancellationToken>())
            .Returns(c2);

        var command = new ReorderCategoriesCommand(new List<(Guid Id, int SortOrder)>
    {
        (c1.Id.Value, 10),
        (c2.Id.Value, 20)
    });

        await _sut.Handle(command, CancellationToken.None);

        c1.SortOrder.ShouldBe(10);
        c2.SortOrder.ShouldBe(20);
        _repository.Received(2).Update(Arg.Any<Categories>(), Arg.Any<byte[]?>());
    }
}
