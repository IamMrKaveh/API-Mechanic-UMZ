using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;

namespace Application.Category.Features.Commands.DeleteCategory;

public class DeleteCategoryHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeleteCategoryHandler> logger) : IRequestHandler<DeleteCategoryCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var categoryId = CategoryId.From(request.Id);
        var category = await categoryRepository.GetByIdAsync(categoryId, ct);
        if (category is null)
            return ServiceResult.NotFound("دسته‌بندی یافت نشد.");

        if (await categoryRepository.HasChildrenAsync(categoryId, ct))
            return ServiceResult.Conflict("دسته‌بندی دارای زیردسته است و قابل حذف نیست.");

        category.Deactivate();
        categoryRepository.Update(category);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Category {Id} deleted", request.Id);
        return ServiceResult.Success();
    }
}