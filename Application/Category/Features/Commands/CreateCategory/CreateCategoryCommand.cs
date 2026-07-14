using Application.Category.Features.Shared;

namespace Application.Category.Features.Commands.CreateCategory;

public record CreateCategoryCommand(
    string CategoryName,
    string? Slug,
    string? Description,
    int SortOrder = 0) : ICommand<CategoryDto>, IAuditableCommand
{
    public string AuditEventType => "Category";

    public string AuditAction => "CreateCategory";

    public string? AuditEntityType => "Category";

    public string? AuditEntityId => null;
}