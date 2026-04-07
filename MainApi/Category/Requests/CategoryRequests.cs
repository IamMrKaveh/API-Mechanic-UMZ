namespace Presentation.Category.Requests;

public record CreateCategoryRequest(
    string Name,
    string? Slug,
    string? Description,
    Guid? ParentCategoryId,
    int SortOrder = 0
);

public record UpdateCategoryRequest(
    string Name,
    string? Slug,
    string? Description,
    int SortOrder,
    string RowVersion
);

public record ReorderCategoriesRequest(IReadOnlyList<Guid> CategoryIds);