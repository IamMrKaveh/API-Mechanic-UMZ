namespace Application.Brand.Features.Commands.UpdateBrand;

public class UpdateBrandHandler : IRequestHandler<UpdateBrandCommand, ServiceResult>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaService _mediaService;
    private readonly IMediaQueryService _mediaQueryService;
    private readonly ILogger<UpdateBrandHandler> _logger;

    public UpdateBrandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IMediaService mediaService,
        IMediaQueryService mediaQueryService,
        ILogger<UpdateBrandHandler> logger
        )
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _mediaService = mediaService;
        _mediaQueryService = mediaQueryService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        UpdateBrandCommand request,
        CancellationToken ct
        )
    {
        var category = await _categoryRepository.GetByIdWithGroupsAsync(request.CategoryId, ct);
        if (category == null)
            return ServiceResult.Failure("دسته‌بندی یافت نشد.", 404);

        try
        {
            category.RenameBrand(request.BrandId, request.Name, request.Description);

            Domain.Media.Media? newMedia = null;
            if (request.IconFile != null)
            {
                var existingMedia = (await _mediaQueryService.GetEntityMediaAsync("CategoryGroup", request.BrandId))
                    .FirstOrDefault(m => m.IsPrimary);

                if (existingMedia != null)
                    await _mediaService.DeleteMediaAsync(existingMedia.Id);

                newMedia = await _mediaService.AttachFileToEntityAsync(
                    request.IconFile.Content,
                    request.IconFile.FileName,
                    request.IconFile.ContentType,
                    request.IconFile.Length,
                    "CategoryGroup",
                    request.BrandId,
                    isPrimary: true);
            }

            _categoryRepository.Update(category);

            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
                return ServiceResult.Success();
            }
            catch (ConcurrencyException)
            {
                if (newMedia != null)
                    await _mediaService.DeleteMediaAsync(newMedia.Id);

                return ServiceResult.Failure("این رکورد توسط کاربر دیگری تغییر یافته است.");
            }
        }
        catch (BrandNotFoundException)
        {
            return ServiceResult.Failure("گروه یافت نشد.", 404);
        }
        catch (DuplicateBrandNameException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}