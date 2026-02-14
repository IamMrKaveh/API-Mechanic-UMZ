namespace Application.Media.Features.Queries.GetMediaById;

public record GetMediaByIdQuery(int MediaId)
    : IRequest<ServiceResult<MediaDetailDto?>>;