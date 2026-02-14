namespace Application.Media.Features.Commands.DeleteMedia;

public class DeleteMediaHandler : IRequestHandler<DeleteMediaCommand, ServiceResult>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly MediaDomainService _mediaDomainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteMediaHandler> _logger;

    public DeleteMediaHandler(
        IMediaRepository mediaRepository,
        MediaDomainService mediaDomainService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteMediaHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _mediaDomainService = mediaDomainService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        DeleteMediaCommand request, CancellationToken cancellationToken)
    {
        var media = await _mediaRepository.GetByIdAsync(request.Id, cancellationToken);
        if (media == null)
            return ServiceResult.Failure("رسانه یافت نشد.", 404);

        var wasPrimary = media.IsPrimary;
        var entityType = media.EntityType;
        var entityId = media.EntityId;

        // حذف نرم از طریق Aggregate (Domain Event ثبت می‌شود)
        media.Delete(request.DeletedBy);
        _mediaRepository.Update(media);

        // اگر Primary بود، رسانه بعدی را Primary کن
        if (wasPrimary)
        {
            var remainingMedias = await _mediaRepository.GetByEntityAsync(
                entityType, entityId, cancellationToken);

            var remaining = remainingMedias.Where(m => m.Id != request.Id).ToList();
            var newPrimary = _mediaDomainService.SelectNewPrimaryAfterDeletion(remaining);

            if (newPrimary != null)
            {
                newPrimary.SetAsPrimary();
                _mediaRepository.Update(newPrimary);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("رسانه {MediaId} با موفقیت حذف شد.", request.Id);
        return ServiceResult.Success();
    }
}