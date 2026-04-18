namespace Application.Media.Features.Queries.GetEntityMedia;

public class GetEntityMediaValidator : AbstractValidator<GetEntityMediaQuery>
{
    public GetEntityMediaValidator()
    {
        RuleFor(x => x.EntityType)
            .NotEmpty().WithMessage("نوع موجودیت الزامی است.");

        RuleFor(x => x.EntityId)
            .NotEmpty().WithMessage("شناسه موجودیت الزامی است.");
    }
}