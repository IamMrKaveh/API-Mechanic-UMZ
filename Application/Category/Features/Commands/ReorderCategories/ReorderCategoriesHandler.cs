using Application.Category.Adapters;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Category.Features.Commands.ReorderCategories;

public class ReorderCategoriesHandler(
    ICategoryRepository categoryRepository,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ReorderCategoriesCommand>
{
    public async Task<ServiceResult> Handle(ReorderCategoriesCommand request, CancellationToken ct)
    {
        foreach (var (Id, SortOrder) in request.Items)
        {
            var categoryId = CategoryId.From(Id);
            var category = await categoryRepository.GetByIdAsync(categoryId, ct);

            if (category is null)
                continue;

            var uniquenessChecker = new CategoryUniquenessCheckerAdapter(categoryRepository);
            await category.UpdateDetails(category.Name, category.Slug, uniquenessChecker, category.Description, SortOrder, dateTimeProvider.UtcNow, ct);
            categoryRepository.Update(category);
        }

        await auditService.LogAsync(
            "Category",
            "ReorderCategories",
            IpAddress.Unknown,
            entityType: "Category");

        return ServiceResult.Success();
    }
}