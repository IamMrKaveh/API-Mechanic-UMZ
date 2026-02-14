namespace Application.Payment.Features.Queries.GetPaymentStatus;

public class GetPaymentStatusHandler : IRequestHandler<GetPaymentStatusQuery, ServiceResult<PaymentStatusDto>>
{
    private readonly IPaymentTransactionRepository _repository;
    private readonly IMapper _mapper;

    public GetPaymentStatusHandler(IPaymentTransactionRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<PaymentStatusDto>> Handle(GetPaymentStatusQuery request, CancellationToken cancellationToken)
    {
        var tx = await _repository.GetByAuthorityAsync(request.Authority, cancellationToken);
        if (tx == null)
        {
            return ServiceResult<PaymentStatusDto>.Failure("تراکنش یافت نشد.");
        }

        var dto = _mapper.Map<PaymentStatusDto>(tx);
        return ServiceResult<PaymentStatusDto>.Success(dto);
    }
}