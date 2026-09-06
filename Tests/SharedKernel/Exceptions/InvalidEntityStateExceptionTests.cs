using SharedKernel.Exceptions;

namespace Tests.SharedKernel.Exceptions;

public class InvalidEntityStateExceptionTests
{
    [Fact]
    public void Constructor_PreservesAllStateDetails()
    {
        var ex = new InvalidEntityStateException("Order", "order-1", "Paid", "Pending");

        ex.EntityName.ShouldBe("Order");
        ex.EntityId.ShouldBe("order-1");
        ex.CurrentState.ShouldBe("Paid");
        ex.ExpectedState.ShouldBe("Pending");
    }

    [Fact]
    public void Constructor_BuildsPersianMessageFromDetails()
    {
        var ex = new InvalidEntityStateException("سفارش", "123", "پرداخت‌شده", "در انتظار");

        ex.Message.ShouldBe("سفارش با شناسه 123 در وضعیت پرداخت‌شده است اما باید در وضعیت در انتظار باشد.");
    }

    [Fact]
    public void ErrorCode_IsAlwaysInvalidEntityState()
    {
        new InvalidEntityStateException("E", "1", "A", "B")
            .ErrorCode.ShouldBe("INVALID_ENTITY_STATE");
    }

    [Fact]
    public void Exception_IsADomainException()
    {
        new InvalidEntityStateException("E", "1", "A", "B")
            .ShouldBeAssignableTo<DomainException>();
    }
}
