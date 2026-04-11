using Application.Category.Adapters;
using Application.Category.Features.Commands.UpdateCategory;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.Common.ValueObjects;

namespace Application.Category.Features.Commands.ReorderCategories;

public class ReorderCategoriesHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateCategoryHandler> logger) : IRequestHandler<ReorderCategoriesCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(ReorderCategoriesCommand request, CancellationToken ct)
    {
        var uniquenessChecker = new CategoryUniquenessCheckerAdapter(categoryRepository);

        foreach (var (Id, SortOrder) in request.Items)
        {
            var categoryId = CategoryId.From(Id);
            var category = await categoryRepository.GetByIdAsync(categoryId, ct);
            if (category is null)
                continue;

            var slug = Slug.FromString(category.Slug.Value);
            category.UpdateDetails(category.Name, slug, uniquenessChecker, category.Description, SortOrder);
            categoryRepository.Update(category);
        }

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Categories reordered successfully");
        return ServiceResult.Success();
    }
}