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

        foreach (var item in request.Items)
        {
            var categoryId = CategoryId.From(item.Id.Value);
            var category = await categoryRepository.GetByIdAsync(categoryId, ct);
            if (category is null)
                continue;

            var slug = Slug.FromString(category.Slug);
            var categoryName = CategoryName.Create(category.Name);

            category.UpdateDetails(categoryName, slug, uniquenessChecker, category.Description, item.SortOrder);
            categoryRepository.Update(category);
        }

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Categories re-ordered successfuly");
        return ServiceResult.Success();
    }
}