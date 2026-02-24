namespace Application.Media.Features.Queries.GetAllMedia;

public class GetAllMediaValidator : AbstractValidator<GetAllMediaQuery>
{
    public GetAllMediaValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}