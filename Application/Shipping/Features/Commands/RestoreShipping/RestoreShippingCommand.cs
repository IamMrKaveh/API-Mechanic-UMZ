using Application.Common.Results;
using Domain.Shipping.ValueObjects;

namespace Application.Shipping.Features.Commands.RestoreShipping;

public record RestoreShippingCommand(ShippingId Id, Guid CurrentUserId) : IRequest<ServiceResult>;