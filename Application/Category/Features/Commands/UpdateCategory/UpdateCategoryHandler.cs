using Application.Category.Adapters;
using Application.Category.Features.Shared;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.Common.ValueObjects;

namespace Application.Category.Features.Commands.UpdateCategory;

public class UpdateCategoryHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger<UpdateCategoryHandler> logger) : IRequestHandler<UpdateCategoryCommand, ServiceResult<CategoryDto>>
{
    public async Task<ServiceResult<CategoryDto>> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var categoryId = CategoryId.From(request.Id.Value);
        var category = await categoryRepository.GetByIdAsync(categoryId, ct);
        if (category is null)
            return ServiceResult<CategoryDto>.NotFound("Category not found.");

        if (await categoryRepository.ExistsByNameAsync(request.Name, categoryId, ct))
            return ServiceResult<CategoryDto>.Conflict("Category name already exists.");

        var slug = string.IsNullOrWhiteSpace(request.Slug?.Value)
            ? Slug.GenerateFrom(request.Name)
            : Slug.FromString(request.Slug);

        if (await categoryRepository.ExistsBySlugAsync(slug, categoryId, ct))
            return ServiceResult<CategoryDto>.Conflict("Slug already exists.");

        var categoryName = CategoryName.Create(request.Name);
        var uniquenessChecker = new CategoryUniquenessCheckerAdapter(categoryRepository);

        category.UpdateDetails(categoryName, slug, uniquenessChecker, request.Description, request.SortOrder);

        if (request.IsActive && !category.IsActive)
            category.Activate();
        else if (!request.IsActive && category.IsActive)
            category.Deactivate();

        categoryRepository.Update(category);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Category {Id} Updated", request.Id);
        return ServiceResult<CategoryDto>.Success(mapper.Map<CategoryDto>(category));
    }
}