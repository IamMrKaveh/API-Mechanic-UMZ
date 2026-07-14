namespace Application.Attribute.Features.Commands.DeleteAttributeType;

public record DeleteAttributeTypeCommand(Guid Id) : ICommand, IAuditableCommand
{
	public string AuditEventType => "Attribute";

	public string AuditAction => "DeleteAttributeType";

	public string? AuditEntityType => "AttributeType";

	public string? AuditEntityId => Id.ToString();
}