using Application.Category.Adapters;
using Application.Category.Features.Shared;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.Common.ValueObjects;

namespace Application.Category.Features.Commands.CreateCategory;

public class CreateCategoryHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger<CreateCategoryHandler> logger) : IRequestHandler<CreateCategoryCommand, ServiceResult<CategoryDto>>
{
    public async Task<ServiceResult<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        if (await categoryRepository.ExistsByNameAsync(request.CategoryName, null, ct))
            return ServiceResult<CategoryDto>.Conflict("Category name already exists.");

        var slug = string.IsNullOrWhiteSpace(request.Slug?.Value)
            ? Slug.GenerateFrom(request.CategoryName)
            : Slug.FromString(request.Slug);

        if (await categoryRepository.ExistsBySlugAsync(slug, null, ct))
            return ServiceResult<CategoryDto>.Conflict("Slug already exists.");

        var categoryName = CategoryName.Create(request.CategoryName);
        var uniquenessChecker = new CategoryUniquenessCheckerAdapter(categoryRepository);
        var categoryId = CategoryId.NewId();

        var category = Domain.Category.Aggregates.Category.Create(
            categoryId,
            categoryName,
            slug,
            uniquenessChecker,
            request.Description,
            request.SortOrder);

        await categoryRepository.AddAsync(category, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Category {Name} created", category.Name);
        return ServiceResult<CategoryDto>.Success(mapper.Map<CategoryDto>(category));
    }
}