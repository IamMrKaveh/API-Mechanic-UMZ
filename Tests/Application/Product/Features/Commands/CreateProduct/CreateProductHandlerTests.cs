using Application.Product.Features.Commands.CreateProduct;
using Application.Product.Features.Shared;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using DomainBrand = Domain.Brand.Aggregates.Brand;
using DomainCategory = Domain.Category.Aggregates.Category;
using DomainProduct = Domain.Product.Aggregates.Product;

namespace Tests.Application.Product.Features.Commands.CreateProduct;

public class CreateProductHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>(); private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>(); private readonly IBrandRepository _brandRepository = Substitute.For<IBrandRepository>(); private readonly IMapper _mapper = Substitute.For<IMapper>(); private readonly CreateProductHandler _sut;

    public CreateProductHandlerTests()
    {
        _sut = new CreateProductHandler(
            _productRepository,
            _categoryRepository,
            _brandRepository,
            _mapper);
    }

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ReturnsNotFoundAndDoesNotAddProduct()
    {
        _categoryRepository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns((DomainCategory?)null);

        var command = new CreateProductCommand(Guid.NewGuid(), Guid.NewGuid(), "Sample");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        await _productRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenBrandNotFound_ReturnsNotFoundAndDoesNotAddProduct()
    {
        var category = await new CategoryBuilder().BuildAsync();

        _categoryRepository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(category);
        _brandRepository
            .GetByIdAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>())
            .Returns((DomainBrand?)null);

        var command = new CreateProductCommand(Guid.NewGuid(), Guid.NewGuid(), "Sample");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        await _productRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenSlugAlreadyExists_ReturnsConflictAndDoesNotAddProduct()
    {
        var category = await new CategoryBuilder().BuildAsync();
        var brand = await new BrandBuilder().WithCategoryId(category.Id).BuildAsync();

        _categoryRepository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(category);
        _brandRepository
            .GetByIdAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>())
            .Returns(brand);
        _productRepository
            .ExistsBySlugAsync(Arg.Any<ProductSlug>(), Arg.Any<ProductId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new CreateProductCommand(category.Id.Value, brand.Id.Value, "Duplicate Product");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _productRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenAllValid_CreatesProductAndReturnsSuccessWithMappedDto()
    {
        var category = await new CategoryBuilder().BuildAsync();
        var brand = await new BrandBuilder().WithCategoryId(category.Id).BuildAsync();

        _categoryRepository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(category);
        _brandRepository
            .GetByIdAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>())
            .Returns(brand);
        _productRepository
            .ExistsBySlugAsync(Arg.Any<ProductSlug>(), Arg.Any<ProductId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _mapper
            .Map<ProductDetailDto>(Arg.Any<DomainProduct>())
            .Returns(new ProductDetailDto());

        var command = new CreateProductCommand(category.Id.Value, brand.Id.Value, "Brand New Product");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.CategoryName.ShouldBe(category.Name.Value);
        result.Value.BrandName.ShouldBe(brand.Name.Value);
        await _productRepository.Received(1).AddAsync(Arg.Any<DomainProduct>(), Arg.Any<CancellationToken>());
    }
}
