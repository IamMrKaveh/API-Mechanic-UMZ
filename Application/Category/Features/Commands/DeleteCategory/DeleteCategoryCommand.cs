namespace Application.Category.Features.Commands.DeleteCategory;

public record DeleteCategoryCommand(Guid CategoryId) : ICommand, IAuditableCommand
{
    public string AuditEventType => "Category";

    public string AuditAction => "DeleteCategory";

    public string? AuditEntityType => "Category";

    public string? AuditEntityId => CategoryId.ToString();
}