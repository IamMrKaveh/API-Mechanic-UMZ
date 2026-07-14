namespace Application.Attribute.Features.Commands.UpdateAttributeType;

public record UpdateAttributeTypeCommand(
	Guid Id,
	string? Name,
	string? DisplayName,
	int? SortOrder,
	bool? IsActive) : ICommand, IAuditableCommand
{
	public string AuditEventType => "Attribute";

	public string AuditAction => "UpdateAttributeType";

	public string? AuditEntityType => "AttributeType";

	public string? AuditEntityId => Id.ToString();
}