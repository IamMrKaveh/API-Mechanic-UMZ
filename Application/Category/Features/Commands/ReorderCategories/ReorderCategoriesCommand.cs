namespace Application.Category.Features.Commands.ReorderCategories;

public sealed record CategoryOrderItem(Guid Id, int SortOrder);

public record ReorderCategoriesCommand(ICollection<CategoryOrderItem> Items) : ICommand;
