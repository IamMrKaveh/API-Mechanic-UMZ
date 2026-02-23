namespace Application.Discount.Features.Commands.CreateDiscount;

public class CreateDiscountHandler : IRequestHandler<CreateDiscountCommand, ServiceResult<DiscountCodeDto>>
{
    private readonly IDiscountRepository _discountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public CreateDiscountHandler(
        IDiscountRepository discountRepository,
        IUnitOfWork unitOfWork,
        IHtmlSanitizer htmlSanitizer,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _discountRepository = discountRepository;
        _unitOfWork = unitOfWork;
        _htmlSanitizer = htmlSanitizer;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<ServiceResult<DiscountCodeDto>> Handle(CreateDiscountCommand request, CancellationToken cancellationToken)
    {
        var sanitizedCode = _htmlSanitizer.Sanitize(request.Code).ToUpper().Trim();

        if (await _discountRepository.ExistsByCodeAsync(sanitizedCode, ct: cancellationToken))
        {
            return ServiceResult<DiscountCodeDto>.Failure("کد تخفیف تکراری است.");
        }

        // ایجاد از طریق Factory Method دامین
        var discount = DiscountCode.Create(
            sanitizedCode,
            request.Percentage,
            request.MaxDiscountAmount,
            request.MinOrderAmount,
            request.UsageLimit,
            request.ExpiresAt,
            request.StartsAt,
            request.MaxUsagePerUser
        );

        if (request.Restrictions != null)
        {
            foreach (var r in request.Restrictions)
            {
                discount.AddRestriction(r.RestrictionType, r.EntityId);
            }
        }

        await _discountRepository.AddAsync(discount, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAdminEventAsync("CreateDiscount", _currentUserService.UserId ?? 0, $"Code created: {discount.Code.Value}");

        return ServiceResult<DiscountCodeDto>.Success(_mapper.Map<DiscountCodeDto>(discount));
    }
}