using Application.Common.Validation;
using FluentValidation;

namespace Application.Wallet.Features.Queries.GetWalletsOverview;

public sealed class GetWalletsOverviewValidator : AbstractValidator<GetWalletsOverviewQuery>
{
    public GetWalletsOverviewValidator()
    {
        this.RuleForPagination(
            x => x.Page,
            x => x.PageSize,
            maxPageSize: 200,
            pageMessage: "شماره صفحه باید بزرگ‌تر یا مساوی ۱ باشد.");

        RuleFor(x => x.MinBalance)
            .GreaterThanOrEqualTo(0).When(x => x.MinBalance.HasValue)
            .WithMessage("حداقل موجودی نمی‌تواند منفی باشد.");

        RuleFor(x => x.MaxBalance)
            .GreaterThanOrEqualTo(0).When(x => x.MaxBalance.HasValue)
            .WithMessage("حداکثر موجودی نمی‌تواند منفی باشد.");

        RuleFor(x => x)
            .Must(x => !x.MinBalance.HasValue || !x.MaxBalance.HasValue || x.MinBalance.Value <= x.MaxBalance.Value)
            .WithMessage("حداقل موجودی نباید بیشتر از حداکثر باشد.");

        this.RuleForDateRange(
            x => x.CreatedFrom,
            x => x.CreatedTo,
            "تاریخ شروع نباید بعد از تاریخ پایان باشد.");
    }
}