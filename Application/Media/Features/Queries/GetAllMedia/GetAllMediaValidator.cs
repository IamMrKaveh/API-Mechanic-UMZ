using Application.Common.Validation;

namespace Application.Media.Features.Queries.GetAllMedia;

public class GetAllMediaValidator : AbstractValidator<GetAllMediaQuery>
{
    public GetAllMediaValidator()
    {
        this.RuleForPagination(x => x.Page, x => x.PageSize);
    }
}