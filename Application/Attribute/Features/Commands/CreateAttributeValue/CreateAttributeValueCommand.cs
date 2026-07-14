using Application.Attribute.Features.Shared;

namespace Application.Attribute.Features.Commands.CreateAttributeValue;

public record CreateAttributeValueCommand(
	Guid TypeId,
	string Value,
	string DisplayValue,
	string? HexCode,
	int SortOrder) : ICommand<AttributeValueDto>, IAuditableCommand
{
	public string AuditEventType => "Attribute";

	public string AuditAction => "CreateAttributeValue";

	public string? AuditEntityType => "AttributeValue";

	public string? AuditEntityId => TypeId.ToString();
}