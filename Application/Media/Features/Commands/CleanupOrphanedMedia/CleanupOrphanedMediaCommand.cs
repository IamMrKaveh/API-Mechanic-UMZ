namespace Application.Media.Features.Commands.CleanupOrphanedMedia;

public record CleanupOrphanedMediaCommand : IRequest<ServiceResult<CleanupResultDto>>;

public class CleanupResultDto
{
    public int DeletedFileCount { get; set; }
}