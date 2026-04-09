using Application.Common.Results;

namespace Application.Shipping.Features.Commands.RestoreShipping;

public record RestoreShippingCommand(Guid Id, Guid CurrentUserId) : IRequest<ServiceResult>;