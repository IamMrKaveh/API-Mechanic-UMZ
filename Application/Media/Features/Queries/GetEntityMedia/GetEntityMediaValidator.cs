using Application.Common.Validation;

namespace Application.Media.Features.Queries.GetEntityMedia;

public class GetEntityMediaValidator : AbstractValidator<GetEntityMediaQuery>
{
    public GetEntityMediaValidator()
    {
        RuleFor(x => x.EntityType)
            .NotEmpty().WithMessage("نوع موجودیت الزامی است.");

        this.RuleForRequiredId(x => x.EntityId, "شناسه موجودیت الزامی است.");
    }
}