using Application.Common.Extensions;
using Application.Product.Features.Shared;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;

namespace Application.Product.Features.Commands.UpdateProduct;

public sealed class UpdateProductHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IBrandRepository brandRepository,
    IMapper mapper)
    : ICommandHandler<UpdateProductCommand, ProductDetailDto>
{
    public async Task<ServiceResult<ProductDetailDto>> Handle(
        UpdateProductCommand request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.Id);
        var productResult = await (productRepository.GetByIdAsync(productId, ct)).OrNotFoundAsync("محصول یافت نشد.");
        if (productResult.IsFailure) return productResult.Error;
        var product = productResult.Value;

        var categoryId = CategoryId.From(request.CategoryId);
        var categoryResult = await (categoryRepository.GetByIdAsync(categoryId, ct)).OrNotFoundAsync("دسته‌بندی یافت نشد.");
        if (categoryResult.IsFailure) return categoryResult.Error;
        var category = categoryResult.Value;

        var brandId = BrandId.From(request.BrandId);
        var brandResult = await (brandRepository.GetByIdAsync(brandId, ct)).OrNotFoundAsync("برند یافت نشد.");
        if (brandResult.IsFailure) return brandResult.Error;
        var brand = brandResult.Value;

        var slug = ProductSlug.GenerateFrom(request.Slug);

        product.UpdateDetails(
            ProductName.Create(request.Name),
            slug,
            request.Description ?? string.Empty);

        product.ChangeBrand(brandId);
        product.ChangeCategory(categoryId);

        if (request.IsActive)
            product.Activate();
        else
            product.Deactivate();

        if (request.IsFeatured)
            product.MarkAsFeatured();
        else
            product.UnmarkAsFeatured();

        productRepository.Update(product, request.RowVersion.FromBase64RowVersion());

        var dto = mapper.Map<ProductDetailDto>(product) with
        {
            CategoryName = category.Name.Value,
            BrandName = brand.Name.Value
        };

        return ServiceResult<ProductDetailDto>.Success(dto);
    }
}
