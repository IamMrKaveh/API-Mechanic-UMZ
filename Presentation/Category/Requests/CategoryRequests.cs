namespace Presentation.Category.Requests;

public record CreateCategoryRequest(
    string Name,
    string? Slug,
    string? Description,
    int SortOrder = 0
);

public record UpdateCategoryRequest(
    string Name,
    string? Slug,
    string? Description,
    int SortOrder,
    bool IsActive,
    string RowVersion
);

public record ReorderCategoriesRequest(IReadOnlyList<CategoryOrderItemRequest> Items);

public record CategoryOrderItemRequest(Guid CategoryId, int SortOrder);