using Application.Audit.Contracts;
using Application.Common.Results;
using Application.Discount.Features.Shared;
using Domain.Common.Interfaces;
using Domain.Discount.Aggregates;
using Domain.Discount.Interfaces;
using SharedKernel.Contracts;

namespace Application.Discount.Features.Commands.CreateDiscount;

public class CreateDiscountHandler(
    IDiscountRepository discountRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    IMapper mapper) : IRequestHandler<CreateDiscountCommand, ServiceResult<DiscountCodeDto>>
{
    private readonly IDiscountRepository _discountRepository = discountRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IMapper _mapper = mapper;

    public async Task<ServiceResult<DiscountCodeDto>> Handle(
        CreateDiscountCommand request,
        CancellationToken ct)
    {
        if (await _discountRepository.ExistsByCodeAsync(request.Code, null, ct))
        {
            return ServiceResult<DiscountCodeDto>.Conflict("کد تخفیف تکراری است.");
        }

        var discount = DiscountCode.Create(
            request.Code,
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

        await _discountRepository.AddAsync(discount, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogAdminEventAsync(
            "CreateDiscount",
            _currentUserService.CurrentUser.UserId,
            $"Code created: {discount.Code}");

        return ServiceResult<DiscountCodeDto>.Success(_mapper.Map<DiscountCodeDto>(discount));
    }
}