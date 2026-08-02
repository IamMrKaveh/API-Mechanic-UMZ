namespace Application.Audit.Features.Queries.GetAuditStatistics;

public sealed class GetAuditStatisticsValidator : AbstractValidator<GetAuditStatisticsQuery>
{
    private const int MaxPageSize = 200;
    private const int MaxRangeDays = 366;

    public GetAuditStatisticsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("شماره صفحه باید بزرگتر از صفر باشد.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("اندازه صفحه باید بزرگتر از صفر باشد.")
            .LessThanOrEqualTo(MaxPageSize)
                .WithMessage($"اندازه صفحه نمی‌تواند بیش از {MaxPageSize} باشد.");

        RuleFor(x => x)
            .Must(x => x.From is null || x.To is null || x.From <= x.To)
            .WithMessage("تاریخ شروع باید کوچکتر یا مساوی تاریخ پایان باشد.")
            .WithName("DateRange");

        RuleFor(x => x)
            .Must(x => !x.From.HasValue || !x.To.HasValue
                       || (x.To.Value - x.From.Value).TotalDays <= MaxRangeDays)
            .WithMessage($"بازه زمانی نمی‌تواند از {MaxRangeDays} روز بیشتر باشد.")
            .WithName("DateRangeSpan");

        RuleFor(x => x.From)
            .LessThanOrEqualTo(_ => DateTime.UtcNow)
            .WithMessage("تاریخ شروع نمی‌تواند در آینده باشد.")
            .When(x => x.From.HasValue);

        RuleFor(x => x.To)
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddDays(1))
            .WithMessage("تاریخ پایان نمی‌تواند در آینده باشد.")
            .When(x => x.To.HasValue);
    }
}
