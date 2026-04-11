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
using Microsoft.Extensions.Logging;

namespace Application.Product.Features.Commands.CreateProduct;

public sealed class CreateProductHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IBrandRepository brandRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    IMapper mapper,
    ILogger<CreateProductHandler> logger) : IRequestHandler<CreateProductCommand, ServiceResult<ProductDetailDto>>
{
    public async Task<ServiceResult<ProductDetailDto>> Handle(
        CreateProductCommand request,
        CancellationToken ct)
    {
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

        if (await productRepository.ExistsBySlugAsync(slug, null, ct))
            return ServiceResult<ProductDetailDto>.Conflict("محصولی با این Slug قبلاً ثبت شده است.");

        var product = Domain.Product.Aggregates.Product.Create(
            ProductId.NewId(),
            ProductName.Create(request.Name),
            slug,
            request.Description,
            categoryId,
            brandId);

        await productRepository.AddAsync(product, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Product {ProductName} created with ID {ProductId}", product.Name, product.Id.Value);

        await auditService.LogProductEventAsync(
            product.Id,
            "CreateProduct",
            $"Product '{product.Name}' created.",
            UserId.From(request.CreatedByUserId));

        var dto = mapper.Map<ProductDetailDto>(product) with
        {
            CategoryName = category.Name.Value,
            BrandName = brand.Name.Value
        };

        return ServiceResult<ProductDetailDto>.Success(dto);
    }
}