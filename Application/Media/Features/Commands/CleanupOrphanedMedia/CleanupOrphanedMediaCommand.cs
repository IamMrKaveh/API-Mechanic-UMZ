namespace Application.Media.Features.Commands.CleanupOrphanedMedia;

public record CleanupOrphanedMediaCommand : IRequest<ServiceResult<int>>;