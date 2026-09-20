using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Category.Features.Commands.DeleteCategory;

public class DeleteCategoryHandler(
    ICategoryRepository categoryRepository,
    ICacheService cacheService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<DeleteCategoryCommand>
{
    public async Task<ServiceResult> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var categoryId = CategoryId.From(request.CategoryId);
        var category = await categoryRepository.GetByIdAsync(categoryId, ct);

        if (category is null)
            return ServiceResult.NotFound("دسته‌بندی یافت نشد.");

        var hasChildren = await categoryRepository.HasBrandAsync(categoryId, ct);
        if (hasChildren)
            return ServiceResult.Failure("دسته‌بندی دارای زیرمجموعه است و قابل حذف نیست.");

        category.Deactivate(dateTimeProvider.UtcNow);
        categoryRepository.Update(category);
        await cacheService.RemoveByPrefixAsync("categories:", ct);

        return ServiceResult.Success();
    }
}