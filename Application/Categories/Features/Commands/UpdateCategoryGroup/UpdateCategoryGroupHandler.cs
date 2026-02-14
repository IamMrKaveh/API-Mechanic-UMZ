namespace Application.Categories.Features.Commands.UpdateCategoryGroup;

public class UpdateCategoryGroupHandler : IRequestHandler<UpdateCategoryGroupCommand, ServiceResult>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaService _mediaService;
    private readonly ILogger<UpdateCategoryGroupHandler> _logger;

    public UpdateCategoryGroupHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IMediaService mediaService,
        ILogger<UpdateCategoryGroupHandler> logger)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _mediaService = mediaService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        UpdateCategoryGroupCommand request, CancellationToken cancellationToken)
    {
        // بارگذاری Aggregate Root
        var category = await _categoryRepository.GetByIdWithGroupsAsync(request.CategoryId, cancellationToken);
        if (category == null)
            return ServiceResult.Failure("دسته‌بندی یافت نشد.", 404);

        try
        {
            // تغییر نام از طریق Aggregate - یکتایی نام بررسی می‌شود
            category.RenameGroup(request.GroupId, request.Name, request.Description);

            // مدیریت آیکون
            Domain.Media.Media? newMedia = null;
            if (request.IconFile != null)
            {
                var existingMedia = (await _mediaService.GetEntityMediaAsync("CategoryGroup", request.GroupId))
                    .FirstOrDefault(m => m.IsPrimary);

                if (existingMedia != null)
                    await _mediaService.DeleteMediaAsync(existingMedia.Id);

                newMedia = await _mediaService.AttachFileToEntityAsync(
                    request.IconFile.Content,
                    request.IconFile.FileName,
                    request.IconFile.ContentType,
                    request.IconFile.Length,
                    "CategoryGroup",
                    request.GroupId,
                    isPrimary: true);
            }

            _categoryRepository.Update(category);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return ServiceResult.Success();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (newMedia != null)
                    await _mediaService.DeleteMediaAsync(newMedia.Id);

                return ServiceResult.Failure("این رکورد توسط کاربر دیگری تغییر یافته است.");
            }
        }
        catch (CategoryGroupNotFoundException)
        {
            return ServiceResult.Failure("گروه یافت نشد.", 404);
        }
        catch (DuplicateCategoryGroupNameException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}