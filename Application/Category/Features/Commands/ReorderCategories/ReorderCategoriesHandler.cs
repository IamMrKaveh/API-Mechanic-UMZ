using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;

namespace Application.Category.Features.Commands.ReorderCategories;

public class ReorderCategoriesHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<ReorderCategoriesCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(ReorderCategoriesCommand request, CancellationToken ct)
    {
        foreach (var item in request.Items)
        {
            var categoryId = CategoryId.From(item.Id);
            var category = await categoryRepository.GetByIdAsync(categoryId, ct);

            if (category is null)
                continue;

            var uniquenessChecker = new Application.Category.Adapters.CategoryUniquenessCheckerAdapter(categoryRepository);
            category.UpdateDetails(category.Name, category.Slug, uniquenessChecker, category.Description, item.SortOrder);
            categoryRepository.Update(category);
        }

        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogAsync(
            "Category",
            "ReorderCategories",
            IpAddress.Unknown,
            entityType: "Category");

        return ServiceResult.Success();
    }
}