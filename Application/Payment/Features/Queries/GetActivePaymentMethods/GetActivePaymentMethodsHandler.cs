using Application.Payment.Contracts;
using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Queries.GetActivePaymentMethods;

public sealed class GetActivePaymentMethodsHandler(
    IPaymentMethodQueryService queryService)
    : IQueryHandler<GetActivePaymentMethodsQuery, IReadOnlyList<AvailablePaymentMethodDto>>
{
    public async Task<ServiceResult<IReadOnlyList<AvailablePaymentMethodDto>>> Handle(
        GetActivePaymentMethodsQuery request,
        CancellationToken ct)
    {
        var items = await queryService.GetActiveAsync(request.OrderAmount, ct);
        return ServiceResult<IReadOnlyList<AvailablePaymentMethodDto>>.Success(items);
    }
}