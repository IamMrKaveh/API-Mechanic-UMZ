namespace Application.Attribute.Features.Commands.UpdateAttributeType;

public record UpdateAttributeTypeCommand(
    Guid Id,
    string? Name,
    string? DisplayName,
    int? SortOrder,
    bool? IsActive) : IRequest<ServiceResult>;