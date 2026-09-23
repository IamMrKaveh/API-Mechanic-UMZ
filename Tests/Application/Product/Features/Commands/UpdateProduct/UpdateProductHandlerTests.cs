using Application.Product.Features.Commands.UpdateProduct;
using Application.Product.Features.Shared;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using BrandAggregate = Domain.Brand.Aggregates.Brand;
using CategoryAggregate = Domain.Category.Aggregates.Category;
using ProductAggregate = Domain.Product.Aggregates.Product;

namespace Tests.Application.Product.Features.Commands.UpdateProduct;

public class UpdateProductHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IBrandRepository _brandRepository = Substitute.For<IBrandRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly UpdateProductHandler _sut;

    public UpdateProductHandlerTests()
    {
        _sut = new UpdateProductHandler(_productRepository, _categoryRepository, _brandRepository, _mapper);
        _mapper.Map<ProductDetailDto>(Arg.Any<ProductAggregate>())
            .Returns(ci =>
            {
                var p = ci.Arg<ProductAggregate>()!;
                return new ProductDetailDto { Id = p.Id.Value, Name = p.Name.Value, Slug = p.Slug.Value };
            });
    }

    private static UpdateProductCommand CommandFor(Guid id, Guid categoryId, Guid brandId) =>
        new(id, categoryId, brandId, "New Name", "new-name", "New desc", true, false, string.Empty);

    [Fact]
    public async Task Handle_WhenProductMissing_ReturnsNotFound()
    {
        _productRepository.GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns((ProductAggregate?)null);

        var result = await _sut.Handle(
            CommandFor(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
        _productRepository.DidNotReceiveWithAnyArgs().Update(default!, default);
    }

    [Fact]
    public async Task Handle_WhenCategoryMissing_ReturnsNotFound()
    {
        var product = new ProductBuilder().Build();
        _productRepository.GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>()).Returns(product);
        _categoryRepository.GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns((CategoryAggregate?)null);

        var result = await _sut.Handle(
            CommandFor(product.Id.Value, Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenBrandMissing_ReturnsNotFound()
    {
        var product = new ProductBuilder().Build();
        var category = await new CategoryBuilder().BuildAsync();
        _productRepository.GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>()).Returns(product);
        _categoryRepository.GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>()).Returns(category);
        _brandRepository.GetByIdAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>())
            .Returns((BrandAggregate?)null);

        var result = await _sut.Handle(
            CommandFor(product.Id.Value, category.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesDetailsAndEnrichesDto()
    {
        var product = new ProductBuilder().WithName("Old").Build();
        var category = await new CategoryBuilder().WithName("Brakes").BuildAsync();
        var brand = await new BrandBuilder().WithName("Bosch").WithCategoryId(category.Id).BuildAsync();
        _productRepository.GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>()).Returns(product);
        _categoryRepository.GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>()).Returns(category);
        _brandRepository.GetByIdAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>()).Returns(brand);

        var result = await _sut.Handle(
            CommandFor(product.Id.Value, category.Id.Value, brand.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        product.Name.Value.ShouldBe("New Name");
        product.CategoryId.ShouldBe(category.Id);
        product.BrandId.ShouldBe(brand.Id);
        result.Value.CategoryName.ShouldBe("Brakes");
        result.Value.BrandName.ShouldBe("Bosch");
        _productRepository.Received(1).Update(product, Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenDeactivatingAndFeaturing_AppliesFlags()
    {
        var product = new ProductBuilder().Build();
        var category = await new CategoryBuilder().BuildAsync();
        var brand = await new BrandBuilder().WithCategoryId(category.Id).BuildAsync();
        _productRepository.GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>()).Returns(product);
        _categoryRepository.GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>()).Returns(category);
        _brandRepository.GetByIdAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>()).Returns(brand);
        var command = new UpdateProductCommand(
            product.Id.Value, category.Id.Value, brand.Id.Value,
            "Name X", "name-x", null, false, true, string.Empty);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        product.IsActive.ShouldBeFalse();
        product.IsFeatured.ShouldBeTrue();
    }
}
