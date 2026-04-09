namespace Application.Shipping.Features.Commands.SetDefaultShipping;

public record SetDefaultShippingCommand(Guid Id) : IRequest<ServiceResult>;