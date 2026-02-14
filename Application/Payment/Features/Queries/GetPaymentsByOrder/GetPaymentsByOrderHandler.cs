namespace Application.Payment.Features.Queries.GetPaymentsByOrder;

public class GetPaymentsByOrderHandler : IRequestHandler<GetPaymentsByOrderQuery, ServiceResult<IEnumerable<PaymentTransactionDto>>>
{
    private readonly IPaymentTransactionRepository _repository;
    private readonly IMapper _mapper;

    public GetPaymentsByOrderHandler(IPaymentTransactionRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IEnumerable<PaymentTransactionDto>>> Handle(GetPaymentsByOrderQuery request, CancellationToken cancellationToken)
    {
        var transactions = await _repository.GetByOrderIdAsync(request.OrderId, cancellationToken);
        var dtos = _mapper.Map<IEnumerable<PaymentTransactionDto>>(transactions);
        return ServiceResult<IEnumerable<PaymentTransactionDto>>.Success(dtos);
    }
}