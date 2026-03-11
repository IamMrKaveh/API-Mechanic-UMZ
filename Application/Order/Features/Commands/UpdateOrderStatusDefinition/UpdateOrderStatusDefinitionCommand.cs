using Application.Common.Models;

namespace Application.Order.Features.Commands.UpdateOrderStatusDefinition;

public record UpdateOrderStatusDefinitionCommand(int Id, UpdateOrderStatusDto Dto) : IRequest<ServiceResult>;