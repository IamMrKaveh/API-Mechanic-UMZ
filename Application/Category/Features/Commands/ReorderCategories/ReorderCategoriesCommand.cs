using Application.Common.Results;

namespace Application.Category.Features.Commands.ReorderCategories;

public record ReorderCategoriesCommand(List<CategoryOrderItem> Items) : IRequest<ServiceResult>;

public record CategoryOrderItem(Guid Id, int SortOrder);