namespace Application.Analytics.Features.Queries.GetRevenueReport;

public sealed class GetRevenueReportValidator : AbstractValidator<GetRevenueReportQuery>
{
    public GetRevenueReportValidator()
    {
        RuleFor(q => q.FromDate)
            .NotEmpty().WithMessage("تاریخ شروع الزامی است.")
            .LessThan(q => q.ToDate).WithMessage("تاریخ شروع باید قبل از تاریخ پایان باشد.");

        RuleFor(q => q.ToDate)
            .NotEmpty().WithMessage("تاریخ پایان الزامی است.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)).WithMessage("تاریخ پایان نمی‌تواند در آینده باشد.");
    }
}