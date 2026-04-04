using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Media.Interfaces;
using Domain.Media.Services;

namespace Application.Media.Features.Commands.SetPrimaryMedia;

public class SetPrimaryMediaHandler(
    IMediaRepository mediaRepository,
    MediaDomainService mediaDomainService,
    IUnitOfWork unitOfWork) : IRequestHandler<SetPrimaryMediaCommand, ServiceResult>
{
    private readonly IMediaRepository _mediaRepository = mediaRepository;
    private readonly MediaDomainService _mediaDomainService = mediaDomainService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult> Handle(
        SetPrimaryMediaCommand request, CancellationToken cancellationToken)
    {
        var media = await _mediaRepository.GetByIdAsync(request.MediaId, cancellationToken);
        if (media == null)
            return ServiceResult.NotFound("رسانه یافت نشد.");

        if (!media.CanBeSetAsPrimary())
            return ServiceResult.Forbidden("این رسانه قابل تنظیم به عنوان اصلی نیست.");

        var allMedias = await _mediaRepository.GetByEntityAsync(
            media.EntityType, media.EntityId, cancellationToken);

        _mediaDomainService.SetPrimaryMedia(media, allMedias);

        foreach (var m in allMedias)
        {
            _mediaRepository.Update(m);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }
}