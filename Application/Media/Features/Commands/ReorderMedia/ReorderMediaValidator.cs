namespace Application.Media.Features.Commands.ReorderMedia;

public class ReorderMediaValidator : AbstractValidator<ReorderMediaCommand>
{
    public ReorderMediaValidator()
    {
        RuleFor(x => x.EntityType)
            .NotEmpty().WithMessage("نوع موجودیت الزامی است.");

        RuleFor(x => x.EntityId)
            .GreaterThan(0).WithMessage("شناسه موجودیت الزامی است.");

        RuleFor(x => x.OrderedMediaIds)
            .NotNull().WithMessage("لیست شناسه‌ها الزامی است.")
            .Must(ids => ids != null && ids.Count > 0).WithMessage("لیست شناسه‌ها نمی‌تواند خالی باشد.")
            .Must(ids => ids != null && ids.Distinct().Count() == ids.Count)
            .WithMessage("شناسه‌های تکراری مجاز نیست.");
    }
}