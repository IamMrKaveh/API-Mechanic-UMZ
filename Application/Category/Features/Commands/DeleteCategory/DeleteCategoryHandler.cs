using Application.Common.Results;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.Common.Interfaces;

namespace Application.Category.Features.Commands.DeleteCategory;

public class DeleteCategoryHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeleteCategoryHandler> logger) : IRequestHandler<DeleteCategoryCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var category = await categoryRepository.GetByIdAsync(CategoryId.From(request.Id), ct);
        if (category is null)
            return ServiceResult.NotFound("دسته‌بندی یافت نشد.");

        if (await categoryRepository.HasChildrenAsync(CategoryId.From(request.Id), ct))
            return ServiceResult.Conflict("امکان حذف دسته‌بندی که زیردسته دارد وجود ندارد.");

        category.Deactivate();
        categoryRepository.Update(category);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Category {Id} deactivated/deleted", request.Id);
        return ServiceResult.Success();
    }
}