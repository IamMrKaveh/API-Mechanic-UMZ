namespace Presentation.Payment.Requests;

public record CreatePaymentMethodRequest(
    string Name,
    string Code,
    string? Description = null,
    string? IconUrl = null,
    decimal FeeAmount = 0m,
    decimal FeePercentage = 0m,
    int SortOrder = 0
);

public record UpdatePaymentMethodRequest(
    string Name,
    string? Description,
    string? IconUrl,
    decimal FeeAmount,
    decimal FeePercentage,
    int SortOrder
);