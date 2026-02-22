namespace Application.Brand.Features.Commands.MoveBrand;

public class MoveBrandValidator : AbstractValidator<MoveBrandCommand>
{
    public MoveBrandValidator()
    {
        RuleFor(x => x.SourceCategoryId).GreaterThan(0).WithMessage("شناسه دسته‌بندی مبدأ الزامی است.");
        RuleFor(x => x.TargetCategoryId).GreaterThan(0).WithMessage("شناسه دسته‌بندی مقصد الزامی است.");
        RuleFor(x => x.BrandId).GreaterThan(0).WithMessage("شناسه گروه الزامی است.");

        RuleFor(x => x)
            .Must(x => x.SourceCategoryId != x.TargetCategoryId)
            .WithMessage("دسته‌بندی مبدأ و مقصد نمی‌توانند یکسان باشند.");
    }
}