namespace Application.Discount.Features.Queries.GetDiscountById;

public class GetDiscountByIdHandler : IRequestHandler<GetDiscountByIdQuery, ServiceResult<DiscountCodeDetailDto?>>
{
    private readonly IDiscountRepository _discountRepository;
    private readonly IMapper _mapper;

    public GetDiscountByIdHandler(
        IDiscountRepository discountRepository,
        IMapper mapper
        )
    {
        _discountRepository = discountRepository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<DiscountCodeDetailDto?>> Handle(
        GetDiscountByIdQuery request,
        CancellationToken ct
        )
    {
        var discount = await _discountRepository.GetByIdWithDetailsAsync(request.Id, ct);
        if (discount == null) return ServiceResult<DiscountCodeDetailDto?>.Failure("Not found");

        var dto = _mapper.Map<DiscountCodeDetailDto>(discount);

        return ServiceResult<DiscountCodeDetailDto?>.Success(dto);
    }
}