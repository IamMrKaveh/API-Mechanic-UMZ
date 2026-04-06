namespace Application.Media.Features.Commands.ReorderMedia;

public class ReorderMediaValidator : AbstractValidator<ReorderMediaCommand>
{
    public ReorderMediaValidator()
    {
        RuleFor(x => x.EntityType).NotEmpty();
        RuleFor(x => x.EntityId).GreaterThan(0);
        RuleFor(x => x.OrderedIds).NotEmpty();
    }
}