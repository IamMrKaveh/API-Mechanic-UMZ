using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.Payment.Services;
using Domain.Payment.ValueObjects;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Fakes;

namespace Tests.Domain.Payment.Services;

public class PaymentSettlementServiceTests
{
    private static (FakeOrderPaymentContext order, PaymentTransaction payment) BuildPaidOrderAndSuccessfulPayment()
    {
        var orderId = OrderId.NewId();
        var order = new FakeOrderPaymentContext { Id = orderId, IsPaid = true, StatusDisplayName = "Paid" };
        var payment = new PaymentTransactionBuilder().WithOrderId(orderId).WithAmount(50_000m).Build();
        payment.MarkAsSuccess(refId: 12345, DateTime.UtcNow);
        return (order, payment);
    }

    [Fact]
    public void ValidateRefundEligibility_WithNullOrder_ThrowsArgumentNullException()
    {
        var payment = new PaymentTransactionBuilder().Build();

        Should.Throw<ArgumentNullException>(() =>
            PaymentSettlementService.ValidateRefundEligibility(null!, payment));
    }

    [Fact]
    public void ValidateRefundEligibility_WithNullPayment_ThrowsArgumentNullException()
    {
        var order = new FakeOrderPaymentContext();

        Should.Throw<ArgumentNullException>(() =>
            PaymentSettlementService.ValidateRefundEligibility(order, null!));
    }

    [Fact]
    public void ValidateRefundEligibility_HappyPath_ReturnsSuccessWithPaymentAmount()
    {
        var (order, payment) = BuildPaidOrderAndSuccessfulPayment();

        var result = PaymentSettlementService.ValidateRefundEligibility(order, payment);

        result.IsValid.ShouldBeTrue();
        result.EligibleAmount.ShouldBe(50_000m);
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void ValidateRefundEligibility_HappyPathWhenDelivered_ReturnsSuccess()
    {
        var (order, payment) = BuildPaidOrderAndSuccessfulPayment();
        order.IsPaid = false;
        order.IsDelivered = true;
        order.StatusDisplayName = "Delivered";

        var result = PaymentSettlementService.ValidateRefundEligibility(order, payment);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ValidateRefundEligibility_WhenOrderNeitherPaidNorDelivered_ReturnsFailure()
    {
        var (order, payment) = BuildPaidOrderAndSuccessfulPayment();
        order.IsPaid = false;
        order.IsDelivered = false;
        order.StatusDisplayName = "Created";

        var result = PaymentSettlementService.ValidateRefundEligibility(order, payment);

        result.IsValid.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrWhiteSpace();
        result.Error.ShouldContain("Created");
        result.EligibleAmount.ShouldBeNull();
    }

    [Fact]
    public void ValidateRefundEligibility_WhenPaymentNotSuccessful_ReturnsFailure()
    {
        var order = new FakeOrderPaymentContext { IsPaid = true };
        var payment = new PaymentTransactionBuilder().WithOrderId(order.Id).Build();

        var result = PaymentSettlementService.ValidateRefundEligibility(order, payment);

        result.IsValid.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ValidateRefundEligibility_WhenPaymentAlreadyRefunded_ReturnsFailure()
    {
        var (order, payment) = BuildPaidOrderAndSuccessfulPayment();
        payment.Refund(DateTime.UtcNow, "prior");

        var result = PaymentSettlementService.ValidateRefundEligibility(order, payment);

        result.IsValid.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ValidateRefundEligibility_WhenPaymentBelongsToDifferentOrder_ReturnsFailure()
    {
        var order = new FakeOrderPaymentContext { Id = OrderId.NewId(), IsPaid = true };
        var payment = new PaymentTransactionBuilder().WithOrderId(OrderId.NewId()).Build();
        payment.MarkAsSuccess(refId: 1, DateTime.UtcNow);

        var result = PaymentSettlementService.ValidateRefundEligibility(order, payment);

        result.IsValid.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ProcessRefund_WithValidInput_RefundsPaymentAndCallsOrderRefundAndReturnsSuccess()
    {
        var (order, payment) = BuildPaidOrderAndSuccessfulPayment();

        var result = PaymentSettlementService.ProcessRefund(order, payment, "customer request");

        result.IsSuccess.ShouldBeTrue();
        result.RefundedAmount.ShouldBe(50_000m);
        result.Error.ShouldBeNull();
        payment.Status.ShouldBe(PaymentStatus.Refunded);
        order.RefundCallCount.ShouldBe(1);
    }

    [Fact]
    public void ProcessRefund_WhenEligibilityFails_ReturnsFailureAndDoesNotMutateAggregates()
    {
        var order = new FakeOrderPaymentContext { IsPaid = false, IsDelivered = false, StatusDisplayName = "Created" };
        var payment = new PaymentTransactionBuilder().WithOrderId(order.Id).Build();
        payment.MarkAsSuccess(refId: 1, DateTime.UtcNow);

        var result = PaymentSettlementService.ProcessRefund(order, payment, "reason");

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrWhiteSpace();
        payment.Status.ShouldBe(PaymentStatus.Success);
        order.RefundCallCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProcessRefund_WithNullOrWhitespaceReason_ThrowsArgumentException(string? reason)
    {
        var (order, payment) = BuildPaidOrderAndSuccessfulPayment();

        Should.Throw<ArgumentException>(() =>
            PaymentSettlementService.ProcessRefund(order, payment, reason!));
    }

    [Fact]
    public void ProcessPaymentSuccess_WithUnpaidOrder_MarksAsPaidAndStartsProcessingAndReturnsSuccess()
    {
        var order = new FakeOrderPaymentContext { IsPaid = false };
        var paymentTxId = PaymentTransactionId.NewId();

        var result = PaymentSettlementService.ProcessPaymentSuccess(order, paymentTxId);

        result.IsSuccess.ShouldBeTrue();
        result.IsIdempotent.ShouldBeFalse();
        order.MarkAsPaidCallCount.ShouldBe(1);
        order.MarkAsPaidWithTransactionId.ShouldBe(paymentTxId);
        order.StartProcessingCallCount.ShouldBe(1);
    }

    [Fact]
    public void ProcessPaymentSuccess_WithAlreadyPaidOrder_ReturnsIdempotentAndDoesNotMutate()
    {
        var order = new FakeOrderPaymentContext { IsPaid = true };

        var result = PaymentSettlementService.ProcessPaymentSuccess(order, PaymentTransactionId.NewId());

        result.IsSuccess.ShouldBeTrue();
        result.IsIdempotent.ShouldBeTrue();
        order.MarkAsPaidCallCount.ShouldBe(0);
        order.StartProcessingCallCount.ShouldBe(0);
    }

    [Fact]
    public void ProcessPaymentSuccess_WithNullOrder_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            PaymentSettlementService.ProcessPaymentSuccess(null!, PaymentTransactionId.NewId()));
    }
}
