namespace Application.Shipping.Features.Commands.CreateShipping;

public class CreateShippingHandler : IRequestHandler<CreateShippingCommand, ServiceResult<ShippingDto>>
{
    private readonly IShippingRepository _shippingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public CreateShippingHandler(
        IShippingRepository shippingRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAuditService auditService,
        ICurrentUserService currentUserService
        )
    {
        _shippingRepository = shippingRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResult<ShippingDto>> Handle(
        CreateShippingCommand request,
        CancellationToken ct
        )
    {
        // Check duplicate name
        if (await _shippingRepository.ExistsByNameAsync(request.Name, ct: ct))
            return ServiceResult<ShippingDto>.Failure("روش ارسال با این نام قبلاً وجود دارد.");

        try
        {
            // Use domain factory
            var shipping = Domain.Shipping.Shipping.Create(
                request.Name,
                Money.FromDecimal(request.Cost),
                request.Description,
                request.EstimatedDeliveryTime,
                request.MinDeliveryDays,
                request.MaxDeliveryDays);

            if (!request.IsActive)
                shipping.Deactivate();

            await _shippingRepository.AddAsync(shipping, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            await _auditService.LogAdminEventAsync(
                "CreateShipping",
                request.CurrentUserId,
                $"Created shipping : {shipping.Name}",
                _currentUserService.IpAddress);

            var resultDto = _mapper.Map<ShippingDto>(shipping);
            return ServiceResult<ShippingDto>.Success(resultDto);
        }
        catch (DomainException ex)
        {
            return ServiceResult<ShippingDto>.Failure(ex.Message);
        }
    }
}