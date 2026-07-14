using Application.Attribute.Features.Shared;

namespace Application.Attribute.Features.Commands.CreateAttributeType;

public record CreateAttributeTypeCommand(
	string Name,
	string DisplayName,
	int SortOrder) : ICommand<AttributeTypeDto>, IAuditableCommand
{
	public string AuditEventType => "Attribute";

	public string AuditAction => "CreateAttributeType";

	public string? AuditEntityType => "AttributeType";

	public string? AuditEntityId => null;
}