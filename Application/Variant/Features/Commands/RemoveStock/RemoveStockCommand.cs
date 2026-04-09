using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Variant.Features.Commands.RemoveStock;

public record RemoveStockCommand(VariantId VariantId, int Quantity, UserId UserId, string Notes) : IRequest<ServiceResult>;