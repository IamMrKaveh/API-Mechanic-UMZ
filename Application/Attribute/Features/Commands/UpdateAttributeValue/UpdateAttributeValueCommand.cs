namespace Application.Attribute.Features.Commands.UpdateAttributeValue;

public record UpdateAttributeValueCommand(
	Guid Id,
	string? Value,
	string? DisplayValue,
	string? HexCode,
	int? SortOrder,
	bool? IsActive) : ICommand, IAuditableCommand
{
	public string AuditEventType => "Attribute";

	public string AuditAction => "UpdateAttributeValue";

	public string? AuditEntityType => "AttributeValue";

	public string? AuditEntityId => Id.ToString();
}