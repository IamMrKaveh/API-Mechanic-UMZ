namespace Application.Audit.Features.Queries.ExportAuditLogs;

public sealed class ExportAuditLogsValidator : AbstractValidator<ExportAuditLogsQuery>
{
    private const int MinMaxRows = 1;
    private const int MaxMaxRows = 100_000;
    private const int MaxEntityTypeLength = 100;
    private const int MaxEventTypeLength = 100;

    private static readonly HashSet<string> AllowedFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "csv",
        "json",
    };

    private static readonly HashSet<string> AllowedEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Information",
        "Debug",
        "Warning",
        "Error",
        "SecurityEvent",
        "SystemEvent",
        "OrderEvent",
        "PaymentEvent",
        "InventoryEvent",
        "ProductEvent",
        "AdminEvent",
    };

    public ExportAuditLogsValidator()
    {
        RuleFor(x => x.Format)
            .NotEmpty().WithMessage("فرمت خروجی نمی‌تواند خالی باشد.")
            .Must(format => AllowedFormats.Contains(format))
            .WithMessage("فرمت خروجی نامعتبر است. مقادیر مجاز: csv, json.");

        RuleFor(x => x.MaxRows!.Value)
            .InclusiveBetween(MinMaxRows, MaxMaxRows)
            .WithMessage($"MaxRows باید بین {MinMaxRows} و {MaxMaxRows:N0} باشد.")
            .When(x => x.MaxRows.HasValue);

        RuleFor(x => x.EventType)
            .MaximumLength(MaxEventTypeLength)
                .WithMessage($"نوع رویداد نمی‌تواند بیش از {MaxEventTypeLength} کاراکتر باشد.")
            .Must(value => value is null || AllowedEventTypes.Contains(value))
                .WithMessage("نوع رویداد نامعتبر است.")
            .When(x => !string.IsNullOrWhiteSpace(x.EventType));

        RuleFor(x => x.EntityType)
            .MaximumLength(MaxEntityTypeLength)
            .WithMessage($"نوع موجودیت نمی‌تواند بیش از {MaxEntityTypeLength} کاراکتر باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.EntityType));

        RuleFor(x => x)
            .Must(x => x.From is null || x.To is null || x.From <= x.To)
            .WithMessage("تاریخ شروع باید کوچکتر یا مساوی تاریخ پایان باشد.")
            .WithName("DateRange");

        RuleFor(x => x.From)
            .LessThanOrEqualTo(_ => DateTime.UtcNow)
            .WithMessage("تاریخ شروع نمی‌تواند در آینده باشد.")
            .When(x => x.From.HasValue);

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("شناسه کاربر نامعتبر است.")
            .When(x => x.UserId.HasValue);
    }
}
