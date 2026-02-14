namespace Application.Media.Features.Commands.ReorderMedia;

public class ReorderMediaHandler : IRequestHandler<ReorderMediaCommand, ServiceResult>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly MediaDomainService _mediaDomainService;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderMediaHandler(
        IMediaRepository mediaRepository,
        MediaDomainService mediaDomainService,
        IUnitOfWork unitOfWork)
    {
        _mediaRepository = mediaRepository;
        _mediaDomainService = mediaDomainService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> Handle(
        ReorderMediaCommand request, CancellationToken cancellationToken)
    {
        var medias = await _mediaRepository.GetByEntityAsync(
            request.EntityType, request.EntityId, cancellationToken);

        if (!medias.Any())
            return ServiceResult.Failure("رسانه‌ای برای این موجودیت یافت نشد.", 404);

        try
        {
            // تغییر ترتیب از طریق Domain Service
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
            return ServiceResult.Failure(ex.Message);
        }
    }
}