using Application.Common.Results;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.Media.Interfaces;
using Domain.Media.Services;

namespace Application.Media.Features.Commands.ReorderMedia;

public class ReorderMediaHandler(
    IMediaRepository mediaRepository,
    MediaDomainService mediaDomainService,
    IUnitOfWork unitOfWork) : IRequestHandler<ReorderMediaCommand, ServiceResult>
{
    private readonly IMediaRepository _mediaRepository = mediaRepository;
    private readonly MediaDomainService _mediaDomainService = mediaDomainService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult> Handle(
        ReorderMediaCommand request, CancellationToken cancellationToken)
    {
        var medias = await _mediaRepository.GetByEntityAsync(
            request.EntityType, request.EntityId, cancellationToken);

        if (!medias.Any())
            return ServiceResult.NotFound("رسانه‌ای برای این موجودیت یافت نشد.");

        try
        {
            _mediaDomainService.ReorderMedias(medias, request.OrderedMediaIds);

            foreach (var media in medias)
            {
                _mediaRepository.Update(media);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.NotFound(ex.Message);
        }
    }
}