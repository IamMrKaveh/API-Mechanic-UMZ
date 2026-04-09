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
        var categoryId = CategoryId.From(request.Id.Value);
        var category = await categoryRepository.GetByIdAsync(categoryId, ct);
        if (category is null)
            return ServiceResult.NotFound("Category not found.");

        if (await categoryRepository.HasChildrenAsync(categoryId, ct))
            return ServiceResult.Conflict("Cannot delete category with children.");

        category.Deactivate();
        categoryRepository.Update(category);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Category {Id} Deleted", request.Id);
        return ServiceResult.Success();
    }
}