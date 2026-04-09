using Application.Common.Results;
using Domain.Category.ValueObjects;

namespace Application.Category.Features.Commands.ReorderCategories;

public record ReorderCategoriesCommand(List<CategoryOrderItem> Items) : IRequest<ServiceResult>;

public record CategoryOrderItem(CategoryId Id, int SortOrder);