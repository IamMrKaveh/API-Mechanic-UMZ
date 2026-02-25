namespace Application.Brand.Features.Commands.CreateBrand;

public class CreateBrandHandler : IRequestHandler<CreateBrandCommand, ServiceResult<int>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaService _mediaService;
    private readonly ILogger<CreateBrandHandler> _logger;

    public CreateBrandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IMediaService mediaService,
        ILogger<CreateBrandHandler> logger
        )
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _mediaService = mediaService;
        _logger = logger;
    }

    public async Task<ServiceResult<int>> Handle(
        CreateBrandCommand request,
        CancellationToken ct
        )
    {
        var category = await _categoryRepository.GetByIdWithGroupsAsync(request.CategoryId, ct);
        if (category == null)
            return ServiceResult<int>.Failure("دسته‌بندی یافت نشد.", 404);

        try
        {
            var group = category.AddBrand(request.Name, request.Description);

            _categoryRepository.Update(category);
            await _unitOfWork.SaveChangesAsync(ct);

            if (request.IconFile != null)
            {
                try
                {
                    await _mediaService.AttachFileToEntityAsync(
                        request.IconFile.Content,
                        request.IconFile.FileName,
                        request.IconFile.ContentType,
                        request.IconFile.Length,
                        "Brand",
                        group.Id,
                        isPrimary: true);

                    await _unitOfWork.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطا در آپلود آیکون گروه {GroupId}", group.Id);
                }
            }

            return ServiceResult<int>.Success(group.Id);
        }
        catch (DuplicateBrandNameException ex)
        {
            return ServiceResult<int>.Failure(ex.Message);
        }
        catch (DomainException ex)
        {
            return ServiceResult<int>.Failure(ex.Message);
        }
    }
}