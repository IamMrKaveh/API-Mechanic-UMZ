namespace Application.Category.Features.Commands.ReorderCategories;

public record ReorderCategoriesCommand(ICollection<(Guid Id, int SortOrder)> Items) : IRequest<ServiceResult>;