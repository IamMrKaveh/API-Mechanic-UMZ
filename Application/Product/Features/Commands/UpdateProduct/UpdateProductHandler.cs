using Application.Common.Extensions;
using Application.Common.Interfaces;
using Application.Product.Features.Shared;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.Common.ValueObjects;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Application.Product.Features.Commands.UpdateProduct;

public sealed class UpdateProductHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IBrandRepository brandRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IMapper mapper) : IRequestHandler<UpdateProductCommand, ServiceResult<ProductDetailDto>>
{
    public async Task<ServiceResult<ProductDetailDto>> Handle(
        UpdateProductCommand request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.Id);
        var product = await productRepository.GetByIdAsync(productId, ct);
        if (product is null)
            return ServiceResult<ProductDetailDto>.NotFound("محصول یافت نشد.");

        var categoryId = CategoryId.From(request.CategoryId);
        var category = await categoryRepository.GetByIdAsync(categoryId, ct);
        if (category is null)
            return ServiceResult<ProductDetailDto>.NotFound("دسته‌بندی یافت نشد.");

        var brandId = BrandId.From(request.BrandId);
        var brand = await brandRepository.GetByIdAsync(brandId, ct);
        if (brand is null)
            return ServiceResult<ProductDetailDto>.NotFound("برند یافت نشد.");

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slug.GenerateFrom(request.Name)
            : Slug.FromString(request.Slug);

        if (await productRepository.ExistsBySlugAsync(slug, productId, ct))
            return ServiceResult<ProductDetailDto>.Conflict("محصولی با این Slug قبلاً ثبت شده است.");

        try
        {
            productRepository.SetOriginalRowVersion(product, request.RowVersion.FromBase64RowVersion());

            product.UpdateDetails(ProductName.Create(request.Name), slug, request.Description ?? string.Empty);

            if (product.CategoryId != categoryId)
                product.ChangeCategory(categoryId);

            if (product.BrandId != brandId)
                product.ChangeBrand(brandId);

            if (request.IsActive && !product.IsActive) product.Activate();
            else if (!request.IsActive && product.IsActive) product.Deactivate();

            if (request.IsFeatured && !product.IsFeatured) product.MarkAsFeatured();
            else if (!request.IsFeatured && product.IsFeatured) product.UnmarkAsFeatured();

            productRepository.Update(product);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException("اطلاعات محصول توسط کاربر دیگری تغییر کرده است.");
        }

        await auditService.LogProductEventAsync(
            product.Id,
            "UpdateProduct",
            $"محصول '{product.Name}' ویرایش شد. Slug='{product.Slug}', IsActive={product.IsActive}, IsFeatured={product.IsFeatured}.",
            UserId.From(request.UpdatedByUserId));

        var dto = mapper.Map<ProductDetailDto>(product) with
        {
            CategoryName = category.Name.Value,
            BrandName = brand.Name.Value
        };

        return ServiceResult<ProductDetailDto>.Success(dto);
    }
}