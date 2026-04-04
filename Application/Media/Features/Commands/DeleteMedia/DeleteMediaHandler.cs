using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Media.Interfaces;
using Domain.Media.Services;

namespace Application.Media.Features.Commands.DeleteMedia;

public class DeleteMediaHandler(
    IMediaRepository mediaRepository,
    MediaDomainService mediaDomainService,
    IUnitOfWork unitOfWork,
    ILogger<DeleteMediaHandler> logger) : IRequestHandler<DeleteMediaCommand, ServiceResult>
{
    private readonly IMediaRepository _mediaRepository = mediaRepository;
    private readonly MediaDomainService _mediaDomainService = mediaDomainService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<DeleteMediaHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        DeleteMediaCommand request, CancellationToken cancellationToken)
    {
        var media = await _mediaRepository.GetByIdAsync(request.Id, cancellationToken);
        if (media == null)
            return ServiceResult.NotFound("رسانه یافت نشد.");

        var wasPrimary = media.IsPrimary;
        var entityType = media.EntityType;
        var entityId = media.EntityId;

        media.Delete(request.DeletedBy);
        _mediaRepository.Update(media);

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