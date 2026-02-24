namespace Application.Analytics.Features.Queries.GetSalesChartData;

public sealed class GetSalesChartDataValidator : AbstractValidator<GetSalesChartDataQuery>
{
    private static readonly string[] AllowedGroupByValues = ["day", "week", "month"];

    public GetSalesChartDataValidator()
    {
        RuleFor(q => q.FromDate)
            .NotEmpty().WithMessage("تاریخ شروع الزامی است.")
            .LessThan(q => q.ToDate).WithMessage("تاریخ شروع باید قبل از تاریخ پایان باشد.");

        RuleFor(q => q.ToDate)
            .NotEmpty().WithMessage("تاریخ پایان الزامی است.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)).WithMessage("تاریخ پایان نمی‌تواند در آینده باشد.");

        RuleFor(q => q.GroupBy)
            .Must(v => AllowedGroupByValues.Contains(v.ToLowerInvariant()))
            .WithMessage("مقدار groupBy باید یکی از day, week یا month باشد.");
    }
}