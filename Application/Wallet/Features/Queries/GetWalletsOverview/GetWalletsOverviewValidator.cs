using FluentValidation;

namespace Application.Wallet.Features.Queries.GetWalletsOverview;

public sealed class GetWalletsOverviewValidator : AbstractValidator<GetWalletsOverviewQuery>
{
    public GetWalletsOverviewValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("شماره صفحه باید بزرگ‌تر یا مساوی ۱ باشد.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200).WithMessage("اندازه صفحه باید بین ۱ تا ۲۰۰ باشد.");

        RuleFor(x => x.MinBalance)
            .GreaterThanOrEqualTo(0).When(x => x.MinBalance.HasValue)
            .WithMessage("حداقل موجودی نمی‌تواند منفی باشد.");

        RuleFor(x => x.MaxBalance)
            .GreaterThanOrEqualTo(0).When(x => x.MaxBalance.HasValue)
            .WithMessage("حداکثر موجودی نمی‌تواند منفی باشد.");

        RuleFor(x => x)
            .Must(x => !x.MinBalance.HasValue || !x.MaxBalance.HasValue || x.MinBalance.Value <= x.MaxBalance.Value)
            .WithMessage("حداقل موجودی نباید بیشتر از حداکثر باشد.");

        RuleFor(x => x)
            .Must(x => !x.CreatedFrom.HasValue || !x.CreatedTo.HasValue || x.CreatedFrom.Value <= x.CreatedTo.Value)
            .WithMessage("تاریخ شروع نباید بعد از تاریخ پایان باشد.");
    }
}