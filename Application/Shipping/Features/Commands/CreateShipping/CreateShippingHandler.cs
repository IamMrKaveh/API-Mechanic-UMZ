using Application.Audit.Contracts;
using Application.Common.Results;
using Application.Shipping.Features.Shared;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.Common.ValueObjects;
using Domain.Shipping.Interfaces;
using SharedKernel.Contracts;

namespace Application.Shipping.Features.Commands.CreateShipping;

public class CreateShippingHandler(
    IShippingRepository shippingRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IAuditService auditService,
    ICurrentUserService currentUserService) : IRequestHandler<CreateShippingCommand, ServiceResult<ShippingDto>>
{
    private readonly IShippingRepository _shippingRepository = shippingRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<ServiceResult<ShippingDto>> Handle(
        CreateShippingCommand request,
        CancellationToken ct)
    {
        if (await _shippingRepository.ExistsByNameAsync(request.Name, ct: ct))
            return ServiceResult<ShippingDto>.Conflict("روش ارسال با این نام قبلاً وجود دارد.");

        try
        {
            var shipping = Domain.Shipping.Aggregates.Shipping.Create(
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
                _currentUserService.CurrentUser.IpAddress);

            var resultDto = _mapper.Map<ShippingDto>(shipping);
            return ServiceResult<ShippingDto>.Success(resultDto);
        }
        catch (DomainException ex)
        {
            return ServiceResult<ShippingDto>.Unexpected(ex.Message);
        }
    }
}