using Domain.Order.ValueObjects;

namespace Application.Order.Mapping;

internal static class OrderStatusTransitionResolver
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> AllowedAdminTransitions =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Paid"] = ["Processing"],
            ["Processing"] = ["Shipped"],
            ["Shipped"] = ["Delivered"],
            ["Created"] = [],
            ["Reserved"] = [],
            ["Pending"] = [],
            ["Failed"] = [],
            ["Delivered"] = [],
            ["Cancelled"] = [],
            ["Returned"] = [],
            ["Refunded"] = [],
            ["Expired"] = []
        };

    public static IReadOnlyList<string> GetAllowedTransitions(OrderStatusValue status)
    {
        if (status is null)
            return [];

        return AllowedAdminTransitions.TryGetValue(status.Value, out var transitions)
            ? transitions
            : [];
    }
}