using Application.Payment.Contracts;
using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Queries.GetPaymentMethods;

public sealed class GetPaymentMethodsHandler(
    IPaymentMethodQueryService queryService)
    : IQueryHandler<GetPaymentMethodsQuery, IReadOnlyList<PaymentMethodListItemDto>>
{
    public async Task<ServiceResult<IReadOnlyList<PaymentMethodListItemDto>>> Handle(
        GetPaymentMethodsQuery request,
        CancellationToken ct)
    {
        var items = await queryService.GetAllAsync(request.IncludeInactive, request.IncludeDeleted, ct);
        return ServiceResult<IReadOnlyList<PaymentMethodListItemDto>>.Success(items);
    }
}