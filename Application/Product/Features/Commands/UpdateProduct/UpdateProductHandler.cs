using Application.Product.Features.Shared;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.Common.ValueObjects;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Application.Product.Features.Commands.UpdateProduct;

public class UpdateProductHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IBrandRepository brandRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateProductHandler> logger) : IRequestHandler<UpdateProductCommand, ServiceResult<ProductDetailDto>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IBrandRepository _brandRepository = brandRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<UpdateProductHandler> _logger = logger;

    public async Task<ServiceResult<ProductDetailDto>> Handle(
        UpdateProductCommand request,
        CancellationToken ct)
    {
        var product = await _productRepository.GetByIdAsync(ProductId.From(request.Id), ct);
        if (product is null)
            return ServiceResult<ProductDetailDto>.NotFound("محصول یافت نشد.");

        var category = await _categoryRepository.GetByIdAsync(CategoryId.From(request.CategoryId), ct);
        if (category is null)
            return ServiceResult<ProductDetailDto>.NotFound("دسته‌بندی یافت نشد.");

        var brand = await _brandRepository.GetByIdAsync(request.BrandId, ct);
        if (brand is null)
            return ServiceResult<ProductDetailDto>.NotFound("برند یافت نشد.");

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slug.GenerateFrom(request.Name)
            : Slug.FromString(request.Slug);

        if (await _productRepository.ExistsBySlugAsync(slug.Value, ProductId.From(request.Id), ct))
            return ServiceResult<ProductDetailDto>.Conflict("محصولی با این Slug قبلاً ثبت شده است.");

        try
        {
            var rowVersion = Convert.FromBase64String(request.RowVersion);
            _productRepository.SetOriginalRowVersion(product, rowVersion);

            product.UpdateDetails(request.Name, slug.Value, request.Description);

            if (product.CategoryId != request.CategoryId)
                product.ChangeCategory(CategoryId.From(request.CategoryId));

            if (product.BrandId != request.BrandId)
                product.ChangeBrand(BrandId.From(request.BrandId));

            if (request.IsActive && !product.IsActive) product.Activate();
            else if (!request.IsActive && product.IsActive) product.Deactivate();

            if (request.IsFeatured && !product.IsFeatured) product.MarkAsFeatured();
            else if (!request.IsFeatured && product.IsFeatured) product.UnmarkAsFeatured();

            _productRepository.Update(product);
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException("اطلاعات محصول توسط کاربر دیگری تغییر کرده است.");
        }

        _logger.LogInformation("Product {ProductId} updated", product.Id);

        var dto = new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            CategoryId = product.CategoryId,
            CategoryName = category.Name.Value,
            BrandId = product.BrandId,
            BrandName = brand.Name.Value,
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            IsDeleted = false,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };

        return ServiceResult<ProductDetailDto>.Success(dto);
    }
}