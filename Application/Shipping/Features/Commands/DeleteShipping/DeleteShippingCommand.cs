namespace Application.Shipping.Features.Commands.DeleteShipping;

public record DeleteShippingCommand(Guid Id, Guid? DeletedByUserId) : IRequest<ServiceResult>;