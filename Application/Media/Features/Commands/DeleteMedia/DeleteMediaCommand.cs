namespace Application.Media.Features.Commands.DeleteMedia;

public record DeleteMediaCommand(int Id, int? DeletedBy = null) : IRequest<ServiceResult>;