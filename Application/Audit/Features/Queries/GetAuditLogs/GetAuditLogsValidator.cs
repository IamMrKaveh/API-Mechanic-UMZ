using System.Net;

namespace Application.Audit.Features.Queries.GetAuditLogs;

public sealed class GetAuditLogsValidator : AbstractValidator<GetAuditLogsQuery>
{
    private const int MaxPageSize = 200;
    private const int MaxEntityTypeLength = 100;
    private const int MaxActionLength = 200;
    private const int MinKeywordLength = 3;
    private const int MaxKeywordLength = 200;

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

    private static readonly HashSet<string> AllowedSortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "CreatedAt",
        "EventType",
        "Action",
    };

    public GetAuditLogsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("شماره صفحه باید بزرگتر از صفر باشد.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("اندازه صفحه باید بزرگتر از صفر باشد.")
            .LessThanOrEqualTo(MaxPageSize)
                .WithMessage($"اندازه صفحه نمی‌تواند بیش از {MaxPageSize} باشد.");

        RuleFor(x => x.SortBy)
            .Must(sortBy => sortBy is null || AllowedSortColumns.Contains(sortBy))
            .WithMessage("ستون مرتب‌سازی نامعتبر است. مقادیر مجاز: CreatedAt, EventType, Action.");

        RuleFor(x => x.EventType)
            .Must(value => value is null || AllowedEventTypes.Contains(value))
                .WithMessage("نوع رویداد نامعتبر است.")
            .When(x => !string.IsNullOrWhiteSpace(x.EventType));

        RuleFor(x => x.EntityType)
            .MaximumLength(MaxEntityTypeLength)
            .WithMessage($"نوع موجودیت نمی‌تواند بیش از {MaxEntityTypeLength} کاراکتر باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.EntityType));

        RuleFor(x => x.Action)
            .MaximumLength(MaxActionLength)
            .WithMessage($"نام عملیات نمی‌تواند بیش از {MaxActionLength} کاراکتر باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.Action));

        RuleFor(x => x.Keyword)
            .MinimumLength(MinKeywordLength)
                .WithMessage($"کلیدواژه جستجو باید حداقل {MinKeywordLength} کاراکتر باشد.")
            .MaximumLength(MaxKeywordLength)
                .WithMessage($"کلیدواژه جستجو نمی‌تواند بیش از {MaxKeywordLength} کاراکتر باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.Keyword));

        RuleFor(x => x.IpAddress)
            .Must(BeValidIpAddress)
                .WithMessage("آدرس IP نامعتبر است.")
            .When(x => !string.IsNullOrWhiteSpace(x.IpAddress));

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
                .WithMessage("شناسه کاربر نامعتبر است.")
            .When(x => x.UserId.HasValue);

        RuleFor(x => x)
            .Must(x => x.From is null || x.To is null || x.From <= x.To)
            .WithMessage("تاریخ شروع باید کوچکتر یا مساوی تاریخ پایان باشد.")
            .WithName("DateRange");

        RuleFor(x => x.From)
            .LessThanOrEqualTo(_ => DateTime.UtcNow)
            .WithMessage("تاریخ شروع نمی‌تواند در آینده باشد.")
            .When(x => x.From.HasValue);

        RuleFor(x => x.To)
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddDays(1))
            .WithMessage("تاریخ پایان نمی‌تواند در آینده باشد.")
            .When(x => x.To.HasValue);
    }

    private static bool BeValidIpAddress(string? value) => !string.IsNullOrWhiteSpace(value) && IPAddress.TryParse(value, out _);
}
