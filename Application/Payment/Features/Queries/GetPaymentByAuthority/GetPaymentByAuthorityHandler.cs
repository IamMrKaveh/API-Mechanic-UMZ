namespace Application.Payment.Features.Queries.GetPaymentByAuthority;

public class GetPaymentByAuthorityHandler : IRequestHandler<GetPaymentByAuthorityQuery, ServiceResult<PaymentTransactionDto?>>
{
    private readonly IPaymentTransactionRepository _repository;
    private readonly IMapper _mapper;

    public GetPaymentByAuthorityHandler(IPaymentTransactionRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<PaymentTransactionDto?>> Handle(GetPaymentByAuthorityQuery request, CancellationToken cancellationToken)
    {
        var tx = await _repository.GetByAuthorityAsync(request.Authority, cancellationToken);
        if (tx == null)
            return ServiceResult<PaymentTransactionDto?>.Failure("تراکنش یافت نشد.");

        return ServiceResult<PaymentTransactionDto?>.Success(_mapper.Map<PaymentTransactionDto>(tx));
    }
}