using Application.Brand.Features.Commands.MoveBrand;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Brands = Domain.Brand.Aggregates.Brand;
using Categories = Domain.Category.Aggregates.Category;

namespace Tests.Application.Brand.Features.Commands.MoveBrand;

public class MoveBrandHandlerTests
{
    private readonly IBrandRepository _brandRepository = Substitute.For<IBrandRepository>(); private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>(); private readonly MoveBrandHandler _sut;

    public MoveBrandHandlerTests()
    {
        _sut = new MoveBrandHandler(_brandRepository, _categoryRepository);
    }

    [Fact]
    public async Task Handle_WhenBrandNotFound_ReturnsNotFound()
    {
        _brandRepository
            .GetByIdAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>())
            .Returns((Brands?)null);

        var result = await _sut.Handle(
            new MoveBrandCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        await _categoryRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default!, default);
        _brandRepository.DidNotReceive().Update(Arg.Any<Brands>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenTargetCategoryNotFound_ReturnsNotFound()
    {
        var brand = await new BrandBuilder().BuildAsync();
        _brandRepository
            .GetByIdAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>())
            .Returns(brand);
        _categoryRepository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns((Categories?)null);

        var result = await _sut.Handle(
            new MoveBrandCommand(brand.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _brandRepository.DidNotReceive().Update(Arg.Any<Brands>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WithValidCommand_ChangesBrandCategoryAndReturnsSuccess()
    {
        var originalCategoryId = CategoryId.NewId();
        var brand = await new BrandBuilder().WithCategoryId(originalCategoryId).BuildAsync();
        var targetCategory = await new CategoryBuilder().BuildAsync();

        _brandRepository
            .GetByIdAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>())
            .Returns(brand);
        _categoryRepository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(targetCategory);

        var result = await _sut.Handle(
            new MoveBrandCommand(brand.Id.Value, targetCategory.Id.Value),
            CancellationToken.None);

        result.ShouldBeSuccess();
        brand.CategoryId.ShouldBe(targetCategory.Id);
        _brandRepository.Received(1).Update(brand, Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_PassesTargetCategoryIdBuiltFromRequestToCategoryRepository()
    {
        var brand = await new BrandBuilder().BuildAsync();
        var targetCategoryGuid = Guid.NewGuid();
        CategoryId? captured = null;

        _brandRepository
            .GetByIdAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>())
            .Returns(brand);
        _categoryRepository
            .GetByIdAsync(Arg.Do<CategoryId>(x => captured = x), Arg.Any<CancellationToken>())
            .Returns((Categories?)null);

        _ = await _sut.Handle(
            new MoveBrandCommand(brand.Id.Value, targetCategoryGuid),
            CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(targetCategoryGuid);
    }
}
