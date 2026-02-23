namespace Application.Category.Features.Commands.ReorderCategories;

public record ReorderCategoriesCommand(
    IReadOnlyList<int> OrderedCategoryIds
    ) : IRequest<ServiceResult>;