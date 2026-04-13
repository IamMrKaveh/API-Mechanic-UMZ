using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.Common.ValueObjects;

namespace Application.Category.Features.Commands.DeleteCategory;

public class DeleteCategoryHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<DeleteCategoryCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var categoryId = CategoryId.From(request.CategoryId);
        var category = await categoryRepository.GetByIdAsync(categoryId, ct);

        if (category is null)
            return ServiceResult.NotFound("دسته‌بندی یافت نشد.");

        var hasChildren = await categoryRepository.HasChildrenAsync(categoryId, ct);
        if (hasChildren)
            return ServiceResult.Failure("دسته‌بندی دارای زیرمجموعه است و قابل حذف نیست.");

        category.Deactivate();
        categoryRepository.Update(category);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogAsync(
            "Category",
            "DeleteCategory",
            IpAddress.Unknown,
            entityType: "Category",
            entityId: request.CategoryId.ToString(),
            ct: ct);

        return ServiceResult.Success();
    }
}