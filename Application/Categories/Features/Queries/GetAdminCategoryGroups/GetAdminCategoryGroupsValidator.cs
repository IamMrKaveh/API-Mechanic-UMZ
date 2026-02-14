namespace Application.Categories.Features.Queries.GetAdminCategoryGroups;

public class GetAdminCategoryGroupsValidator : AbstractValidator<GetAdminCategoryGroupsQuery>
{
    public GetAdminCategoryGroupsValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}