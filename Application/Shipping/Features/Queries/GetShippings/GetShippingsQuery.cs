using Application.Common.Results;
using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Queries.GetShippings;

public record GetShippingsQuery(bool IncludeInactive = false) : IRequest<ServiceResult<IReadOnlyList<ShippingListItemDto>>>;