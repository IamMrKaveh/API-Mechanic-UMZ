using Application.Common.Results;
using Application.Product.Features.Shared;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.Common.Interfaces;
using Domain.Common.ValueObjects;
using Domain.Product.Aggregates;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;

namespace Application.Product.Features.Commands.CreateProduct;

public class CreateProductHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IBrandRepository brandRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateProductHandler> logger) : IRequestHandler<CreateProductCommand, ServiceResult<ProductDetailDto>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IBrandRepository _brandRepository = brandRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<CreateProductHandler> _logger = logger;

    public async Task<ServiceResult<ProductDetailDto>> Handle(
        CreateProductCommand request,
        CancellationToken ct)
    {
        var category = await _categoryRepository.GetByIdAsync(CategoryId.From(request.CategoryId), ct);
        if (category is null)
            return ServiceResult<ProductDetailDto>.NotFound("دسته‌بندی یافت نشد.");

        var brand = await _brandRepository.GetByIdAsync(request.BrandId, ct);
        if (brand is null)
            return ServiceResult<ProductDetailDto>.NotFound("برند یافت نشد.");

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slug.GenerateFrom(request.Name)
            : Slug.FromString(request.Slug);

        if (await _productRepository.ExistsBySlugAsync(slug.Value, ct: ct))
            return ServiceResult<ProductDetailDto>.Conflict("محصولی با این Slug قبلاً ثبت شده است.");

        var productId = ProductId.NewId();
        var product = Domain.Product.Aggregates.Product.Create(
            productId,
            request.Name,
            slug.Value,
            request.Description,
            CategoryId.From(request.CategoryId),
            BrandId.From(request.BrandId));

        await _productRepository.AddAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductName} created with ID {ProductId}", product.Name, product.Id);

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