namespace Application.Discount.Features.Queries.GetDiscounts;

public class GetDiscountsHandler : IRequestHandler<GetDiscountsQuery, ServiceResult<PaginatedResult<DiscountCodeDto>>>
{
    private readonly IDiscountRepository _discountRepository;
    private readonly IMapper _mapper;

    public GetDiscountsHandler(
        IDiscountRepository discountRepository,
        IMapper mapper
        )
    {
        _discountRepository = discountRepository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<PaginatedResult<DiscountCodeDto>>> Handle(
        GetDiscountsQuery request,
        CancellationToken ct
        )
    {
        var (discounts, total) = await _discountRepository.GetPagedAsync(
            request.IncludeExpired,
            request.IncludeDeleted,
            request.Page,
            request.PageSize,
            ct);

        var dtos = _mapper.Map<List<DiscountCodeDto>>(discounts);

        return ServiceResult<PaginatedResult<DiscountCodeDto>>.Success(
            PaginatedResult<DiscountCodeDto>.Create(dtos, total, request.Page, request.PageSize));
    }
}