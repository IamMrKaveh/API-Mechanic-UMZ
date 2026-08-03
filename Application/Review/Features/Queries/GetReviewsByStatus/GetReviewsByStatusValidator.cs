namespace Application.Review.Features.Queries.GetReviewsByStatus;

public sealed class GetReviewsByStatusValidator : AbstractValidator<GetReviewsByStatusQuery>
{
    private static readonly HashSet<string> AllowedStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "Pending", "Approved", "Rejected", "All" };

    public GetReviewsByStatusValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("پارامتر status الزامی است.")
            .Must(s => AllowedStatuses.Contains(s))
            .WithMessage("پارامتر status نامعتبر است. مقادیر مجاز: Pending، Approved، Rejected، All.");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("شماره صفحه باید بزرگ‌تر از صفر باشد.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("اندازه صفحه باید بین ۱ تا ۱۰۰ باشد.");

        RuleFor(x => x.MinRating!.Value)
            .InclusiveBetween(1, 5)
            .When(x => x.MinRating.HasValue)
            .WithMessage("حداقل امتیاز باید بین ۱ تا ۵ باشد.");

        RuleFor(x => x.SearchText!)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.SearchText))
            .WithMessage("طول متن جست‌وجو نباید بیشتر از ۲۰۰ کاراکتر باشد.");

        RuleFor(x => x)
            .Must(x => !x.DateFrom.HasValue || !x.DateTo.HasValue || x.DateFrom.Value <= x.DateTo.Value)
            .WithMessage("بازه‌ی تاریخ نامعتبر است.");
    }
}
