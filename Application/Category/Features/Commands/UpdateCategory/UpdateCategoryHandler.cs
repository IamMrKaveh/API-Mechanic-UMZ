using Application.Category.Features.Shared;
using Application.Common.Results;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.Common.Interfaces;
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
        var category = await categoryRepository.GetByIdAsync(request.Id, ct);
        if (category is null)
            return ServiceResult<CategoryDto>.NotFound("دسته‌بندی یافت نشد.");

        if (await categoryRepository.ExistsByNameAsync(request.Name, request.Id, ct))
            return ServiceResult<CategoryDto>.Conflict("دسته‌بندی با این نام قبلاً ثبت شده است.");

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slug.GenerateFrom(request.Name)
            : Slug.FromString(request.Slug);

        if (await categoryRepository.ExistsBySlugAsync(slug.Value, request.Id, ct))
            return ServiceResult<CategoryDto>.Conflict("Slug قبلاً استفاده شده است.");

        category.UpdateDetails(request.Name, slug, request.Description, request.SortOrder);

        if (request.IsActive && !category.IsActive)
            category.Activate();
        else if (!request.IsActive && category.IsActive)
            category.Deactivate();

        if (request.ParentCategoryId is not null)
        {
            var newParentId = CategoryId.From(request.ParentCategoryId.Value);
            if (category.ParentCategoryId != newParentId)
                category.MoveToParent(newParentId);
        }
        else if (category.ParentCategoryId is not null)
        {
            category.MoveToParent(null);
        }

        categoryRepository.Update(category);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<CategoryDto>.Success(mapper.Map<CategoryDto>(category));
    }
}