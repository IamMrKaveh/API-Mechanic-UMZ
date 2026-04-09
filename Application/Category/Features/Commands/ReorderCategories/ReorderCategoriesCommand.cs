using Application.Category.Features.Shared;

namespace Application.Category.Features.Commands.ReorderCategories;

public record ReorderCategoriesCommand(List<CategoryOrderItemDto> Items) : IRequest<ServiceResult>;