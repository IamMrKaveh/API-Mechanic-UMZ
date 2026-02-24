namespace Application.Payment.Features.Queries.GetAdminPayments;

public class GetAdminPaymentsHandler : IRequestHandler<GetAdminPaymentsQuery, ServiceResult<PaginatedResult<PaymentTransactionDto>>>
{
    private readonly IPaymentTransactionRepository _repository;
    private readonly IMapper _mapper;

    public GetAdminPaymentsHandler(IPaymentTransactionRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<PaginatedResult<PaymentTransactionDto>>> Handle(GetAdminPaymentsQuery request, CancellationToken cancellationToken)
    {
        var (transactions, total) = await _repository.GetPagedAsync(
            request.Params.OrderId,
            request.Params.UserId,
            request.Params.Status,
            request.Params.FromDate,
            request.Params.ToDate,
            request.Params.Page,
            request.Params.PageSize,
            cancellationToken);

        var dtos = _mapper.Map<IEnumerable<PaymentTransactionDto>>(transactions);

        return ServiceResult<PaginatedResult<PaymentTransactionDto>>.Success(
            PaginatedResult<PaymentTransactionDto>.Create([.. dtos], total, request.Params.Page, request.Params.PageSize));
    }
}