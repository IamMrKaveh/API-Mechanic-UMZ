using Application.Category.Features.Shared;
using Application.Common.Results;
using Domain.Category.Aggregates;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.Common.Interfaces;
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
        if (await categoryRepository.ExistsByNameAsync(request.Name, null, ct))
            return ServiceResult<CategoryDto>.Conflict("دسته‌بندی با این نام قبلاً ثبت شده است.");

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slug.GenerateFrom(request.Name)
            : Slug.FromString(request.Slug);

        if (await categoryRepository.ExistsBySlugAsync(slug.Value, null, ct))
            return ServiceResult<CategoryDto>.Conflict("دسته‌بندی با این Slug قبلاً ثبت شده است.");

        CategoryId? parentId = null;
        if (request.ParentCategoryId.HasValue)
        {
            parentId = CategoryId.From(request.ParentCategoryId.Value);
            var parent = await categoryRepository.GetByIdAsync(parentId, ct);
            if (parent is null)
                return ServiceResult<CategoryDto>.NotFound("دسته‌بندی والد یافت نشد.");
        }

        var category = Category.Create(
            CategoryId.NewId(),
            request.Name,
            slug,
            request.Description,
            parentId,
            request.SortOrder);

        await categoryRepository.AddAsync(category, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Category {Name} created", category.Name);
        return ServiceResult<CategoryDto>.Success(mapper.Map<CategoryDto>(category));
    }
}