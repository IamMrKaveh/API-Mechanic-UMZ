using Application.Payment.Features.Queries.GetAdminPayments;
using Mapster;
using Presentation.Payment.Requests;

namespace Presentation.Payment.Mapping;

public sealed class PaymentMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AdminPaymentSearchRequest, GetAdminPaymentsQuery>();
    }
}