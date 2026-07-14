namespace Application.Attribute.Features.Commands.DeleteAttributeValue;

public record DeleteAttributeValueCommand(Guid Id) : ICommand, IAuditableCommand
{
	public string AuditEventType => "Attribute";

	public string AuditAction => "DeleteAttributeValue";

	public string? AuditEntityType => "AttributeValue";

	public string? AuditEntityId => Id.ToString();
}