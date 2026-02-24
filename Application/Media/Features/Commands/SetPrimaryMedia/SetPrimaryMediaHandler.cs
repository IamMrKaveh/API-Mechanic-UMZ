namespace Application.Media.Features.Commands.SetPrimaryMedia;

public class SetPrimaryMediaHandler : IRequestHandler<SetPrimaryMediaCommand, ServiceResult>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly MediaDomainService _mediaDomainService;
    private readonly IUnitOfWork _unitOfWork;

    public SetPrimaryMediaHandler(
        IMediaRepository mediaRepository,
        MediaDomainService mediaDomainService,
        IUnitOfWork unitOfWork)
    {
        _mediaRepository = mediaRepository;
        _mediaDomainService = mediaDomainService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> Handle(
        SetPrimaryMediaCommand request, CancellationToken cancellationToken)
    {
        var media = await _mediaRepository.GetByIdAsync(request.MediaId, cancellationToken);
        if (media == null)
            return ServiceResult.Failure("رسانه یافت نشد.", 404);

        if (!media.CanBeSetAsPrimary())
            return ServiceResult.Failure("این رسانه قابل تنظیم به عنوان اصلی نیست.");

        
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