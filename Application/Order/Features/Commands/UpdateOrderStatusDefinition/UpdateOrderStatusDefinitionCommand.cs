using Application.Common.Results;

namespace Application.Order.Features.Commands.UpdateOrderStatusDefinition;

public record UpdateOrderStatusDefinitionCommand(
    Guid Id,
    string Name,
    string DisplayName,
    string? Description,
    int SortOrder,
    bool IsDefault) : IRequest<ServiceResult>;