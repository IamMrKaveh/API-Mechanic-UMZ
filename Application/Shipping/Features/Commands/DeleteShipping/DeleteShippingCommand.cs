using Application.Common.Results;
using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Shipping.Features.Commands.DeleteShipping;

public record DeleteShippingCommand(ShippingId Id, UserId CurrentUserId) : IRequest<ServiceResult>;