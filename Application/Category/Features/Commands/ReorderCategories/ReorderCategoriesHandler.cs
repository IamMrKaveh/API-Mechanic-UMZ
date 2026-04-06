using Application.Common.Results;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.Common.Interfaces;

namespace Application.Category.Features.Commands.ReorderCategories;

public class ReorderCategoriesHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ReorderCategoriesCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(ReorderCategoriesCommand request, CancellationToken ct)
    {
        foreach (var item in request.Items)
        {
            var category = await categoryRepository.GetByIdAsync(CategoryId.From(item.Id), ct);
            if (category is null) continue;

            category.UpdateDetails(category.Name, category.Slug, category.Description, item.SortOrder);
            categoryRepository.Update(category);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}