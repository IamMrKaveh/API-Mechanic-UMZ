namespace Application.Review.Features.Commands.RemoveAdminReply;

public sealed class RemoveAdminReplyValidator : AbstractValidator<RemoveAdminReplyCommand>
{
    public RemoveAdminReplyValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
    }
}
