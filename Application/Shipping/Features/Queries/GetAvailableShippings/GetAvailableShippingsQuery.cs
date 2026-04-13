using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Queries.GetAvailableShippings;

public sealed record GetAvailableShippingsQuery(
    decimal OrderAmount) : IRequest<ServiceResult<IReadOnlyList<AvailableShippingDto>>>;