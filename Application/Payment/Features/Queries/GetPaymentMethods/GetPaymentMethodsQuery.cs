using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Queries.GetPaymentMethods;

public record GetPaymentMethodsQuery(
    bool IncludeInactive = false,
    bool IncludeDeleted = false)
    : IQuery<IReadOnlyList<PaymentMethodListItemDto>>, ICacheableQuery
{
    public string CacheKey => $"payment-methods:list:inactive={IncludeInactive}:deleted={IncludeDeleted}";

    public TimeSpan? Expiry => TimeSpan.FromMinutes(30);
}