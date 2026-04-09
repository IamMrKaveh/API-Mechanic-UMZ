using Application.Product.Features.Shared;

namespace Application.Product.Features.Commands.BulkUpdatePrices;

public record BulkUpdatePricesCommand(ICollection<VariantPriceUpdateInput> Updates) : IRequest<ServiceResult>;