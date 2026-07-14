using Application.Category.Features.Shared;

namespace Application.Category.Features.Commands.UpdateCategory;

public record UpdateCategoryCommand(
    Guid Id,
    string Name,
    bool IsActive,
    string? Slug,
    string? Description,
    int SortOrder,
    string? RowVersion) : ICommand<CategoryDto>, IAuditableCommand
{
    public string AuditEventType => "Category";

    public string AuditAction => "UpdateCategory";

    public string? AuditEntityType => "Category";

    public string? AuditEntityId => Id.ToString();
}