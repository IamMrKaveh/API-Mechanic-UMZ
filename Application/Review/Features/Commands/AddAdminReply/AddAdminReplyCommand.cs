namespace Application.Review.Features.Commands.AddAdminReply;

public record AddAdminReplyCommand(Guid ReviewId, string Reply) : IRequest<ServiceResult>;