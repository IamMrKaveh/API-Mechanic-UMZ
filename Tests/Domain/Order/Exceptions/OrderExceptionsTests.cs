using Domain.Order.Exceptions;
using Domain.Order.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Order.Exceptions;

public class OrderExceptionsTests
{
    [Fact]
    public void EmptyOrderException_HasErrorCodeAndDefaultMessage()
    {
        var sut = new EmptyOrderException();

        sut.ErrorCode.ShouldBe("EMPTY_ORDER");
        sut.Message.ShouldContain("at least one item");
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void InvalidOrderTransitionException_ExposesFromAndToStatusesAndErrorCode()
    {
        var sut = new InvalidOrderTransitionException(OrderStatusValue.Created, OrderStatusValue.Delivered);

        sut.FromStatus.ShouldBe(OrderStatusValue.Created);
        sut.ToStatus.ShouldBe(OrderStatusValue.Delivered);
        sut.ErrorCode.ShouldBe("INVALID_ORDER_TRANSITION");
        sut.Message.ShouldContain(OrderStatusValue.Created.DisplayName);
        sut.Message.ShouldContain(OrderStatusValue.Delivered.DisplayName);
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void OrderAlreadyPaidException_ExposesOrderIdAndErrorCode()
    {
        var orderId = OrderId.NewId();

        var sut = new OrderAlreadyPaidException(orderId);

        sut.OrderId.ShouldBe(orderId);
        sut.ErrorCode.ShouldBe("ORDER_ALREADY_PAID");
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void OrderCancellationNotAllowedException_ExposesCurrentStatusAndErrorCode()
    {
        var sut = new OrderCancellationNotAllowedException(OrderStatusValue.Delivered);

        sut.CurrentStatus.ShouldBe(OrderStatusValue.Delivered);
        sut.ErrorCode.ShouldBe("ORDER_CANCELLATION_NOT_ALLOWED");
        sut.Message.ShouldContain(OrderStatusValue.Delivered.DisplayName);
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void OrderNotFoundException_ParameterlessCtor_HasNullOrderIdAndDefaultMessage()
    {
        var sut = new OrderNotFoundException();

        sut.OrderId.ShouldBeNull();
        sut.ErrorCode.ShouldBe("ORDER_NOT_FOUND");
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void OrderNotFoundException_WithOrderId_ExposesId()
    {
        var orderId = OrderId.NewId();

        var sut = new OrderNotFoundException(orderId);

        sut.OrderId.ShouldBe(orderId);
        sut.Message.ShouldContain(orderId.Value.ToString());
    }
}
