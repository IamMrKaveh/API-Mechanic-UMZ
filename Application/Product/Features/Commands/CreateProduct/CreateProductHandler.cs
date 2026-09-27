using Application.Product.Features.Shared;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;

namespace Application.Product.Features.Commands.CreateProduct;

public sealed class CreateProductHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IBrandRepository brandRepository,
    IMapper mapper)
    : ICommandHandler<CreateProductCommand, ProductDetailDto>
{
    public async Task<ServiceResult<ProductDetailDto>> Handle(
        CreateProductCommand request,
        CancellationToken ct)
    {
        var categoryId = CategoryId.From(request.CategoryId);
        var categoryResult = await (categoryRepository.GetByIdAsync(categoryId, ct)).OrNotFoundAsync("دسته‌بندی یافت نشد.");
        if (categoryResult.IsFailure) return categoryResult.Error;
        var category = categoryResult.Value;

        var brandId = BrandId.From(request.BrandId);
        var brandResult = await (brandRepository.GetByIdAsync(brandId, ct)).OrNotFoundAsync("برند یافت نشد.");
        if (brandResult.IsFailure) return brandResult.Error;
        var brand = brandResult.Value;

        var slug = ProductSlug.GenerateFrom(request.Name);

        if (await productRepository.ExistsBySlugAsync(slug, null, ct))
            return ServiceResult<ProductDetailDto>.Conflict("محصولی با این Slug قبلاً ثبت شده است.");

        var product = Domain.Product.Aggregates.Product.Create(
            ProductName.Create(request.Name),
            slug,
            string.Empty,
            brandId,
            categoryId);

        await productRepository.AddAsync(product, ct);

        var dto = mapper.Map<ProductDetailDto>(product) with
        {
            Id = product.Id.Value,
            CategoryName = category.Name.Value,
            BrandName = brand.Name.Value
        };

        return ServiceResult<ProductDetailDto>.Success(dto);
    }
}