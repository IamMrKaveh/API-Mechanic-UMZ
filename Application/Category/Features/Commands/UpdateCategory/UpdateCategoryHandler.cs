using Application.Category.Adapters;
using Application.Category.Features.Shared;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Category.Features.Commands.UpdateCategory;

public class UpdateCategoryHandler(
    ICategoryRepository categoryRepository,
    ICacheService cacheService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateCategoryCommand, CategoryDto>
{
    public async Task<ServiceResult<CategoryDto>> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var categoryId = CategoryId.From(request.Id);
        var categoryResult = await (categoryRepository.GetByIdAsync(categoryId, ct)).OrNotFoundAsync("دسته‌بندی یافت نشد.");
        if (categoryResult.IsFailure) return categoryResult.Error;
        var category = categoryResult.Value;

        var rowVersion = !string.IsNullOrWhiteSpace(request.RowVersion)
            ? Convert.FromBase64String(request.RowVersion)
            : null;

        var name = CategoryName.Create(request.Name);
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? CategorySlug.GenerateFrom(request.Name)
            : CategorySlug.FromString(request.Slug);

        var uniquenessChecker = new CategoryUniquenessCheckerAdapter(categoryRepository);
        await category.UpdateDetails(name, slug, uniquenessChecker, request.Description, request.SortOrder, dateTimeProvider.UtcNow, ct);

        if (request.IsActive && !category.IsActive)
            category.Activate(dateTimeProvider.UtcNow);
        else if (!request.IsActive && category.IsActive)
            category.Deactivate(dateTimeProvider.UtcNow);

        categoryRepository.Update(category, rowVersion);
        await cacheService.RemoveByPrefixAsync("categories:", ct);

        var dto = category.Adapt<CategoryDto>();
        return ServiceResult<CategoryDto>.Success(dto);
    }
}
