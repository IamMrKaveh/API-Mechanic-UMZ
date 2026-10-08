using Application.Payment.Features.Queries.GetAdminPayments;
using Mapster;
using Presentation.Payment.Mapping;
using Presentation.Payment.Requests;

namespace Tests.Presentation.Payment.Mapping;

public class PaymentMappingConfigTests
{
    private readonly TypeAdapterConfig _config = new();
    private readonly PaymentMappingConfig _sut = new();

    public PaymentMappingConfigTests()
    {
        _sut.Register(_config);
        _config.Compile();
    }

    [Fact]
    public void AdminPaymentSearchRequest_MapsToQuery()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var from = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);
        var request = new AdminPaymentSearchRequest(orderId, userId, "Paid", "ZarinPal", from, to);

        // Act
        var query = request.Adapt<GetAdminPaymentsQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.OrderId.ShouldBe(request.OrderId);
        query.UserId.ShouldBe(request.UserId);
        query.Status.ShouldBe(request.Status);
        query.Gateway.ShouldBe(request.Gateway);
        query.FromDate.ShouldBe(request.FromDate);
        query.ToDate.ShouldBe(request.ToDate);
    }

    [Fact]
    public void AdminPaymentSearchRequest_WithDefaults_MapsToQuery()
    {
        // Arrange
        var request = new AdminPaymentSearchRequest();

        // Act
        var query = request.Adapt<GetAdminPaymentsQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.OrderId.ShouldBeNull();
        query.UserId.ShouldBeNull();
        query.Status.ShouldBeNull();
        query.Gateway.ShouldBeNull();
        query.FromDate.ShouldBeNull();
        query.ToDate.ShouldBeNull();
    }

    [Fact]
    public void PaymentMappingConfig_ImplementsIRegister()
    {
        _sut.ShouldBeAssignableTo<IRegister>();
    }
}
