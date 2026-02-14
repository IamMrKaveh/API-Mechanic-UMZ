namespace Application.Order.Features.Commands.CreateShippingMethod;

public class CreateShippingMethodHandler : IRequestHandler<CreateShippingMethodCommand, ServiceResult<ShippingMethodDto>>
{
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public CreateShippingMethodHandler(
        IShippingMethodRepository shippingMethodRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _shippingMethodRepository = shippingMethodRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResult<ShippingMethodDto>> Handle(
        CreateShippingMethodCommand request,
        CancellationToken ct)
    {
        // Check duplicate name
        if (await _shippingMethodRepository.ExistsByNameAsync(request.Name, ct: ct))
            return ServiceResult<ShippingMethodDto>.Failure("روش ارسال با این نام قبلاً وجود دارد.");

        try
        {
            // Use domain factory method
            var method = ShippingMethod.Create(
                request.Name,
                Money.FromDecimal(request.Cost),
                request.Description,
                request.EstimatedDeliveryTime,
                request.MinDeliveryDays,
                request.MaxDeliveryDays);

            if (!request.IsActive)
                method.Deactivate();

            await _shippingMethodRepository.AddAsync(method, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            await _auditService.LogAdminEventAsync(
                "CreateShippingMethod",
                request.CurrentUserId,
                $"Created shipping method: {method.Name}",
                _currentUserService.IpAddress);

            var resultDto = _mapper.Map<ShippingMethodDto>(method);
            return ServiceResult<ShippingMethodDto>.Success(resultDto);
        }
        catch (DomainException ex)
        {
            return ServiceResult<ShippingMethodDto>.Failure(ex.Message);
        }
    }
}