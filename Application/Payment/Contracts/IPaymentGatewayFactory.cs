namespace Application.Payment.Contracts;

public interface IPaymentGatewayFactory
{
    IPaymentGateway GetGateway(string gatewayName = "zarinpal");

    IReadOnlyList<string> GetAvailableGateways();
}