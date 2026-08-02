using Domain.Variant.Exceptions;
using Domain.Variant.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Variant.ValueObjects;

public class InvalidVariantOperationExceptionTests
{
    [Fact]
    public void Construction_ExposesAllConstructorArgumentsAsProperties()
    {
        var id = VariantId.NewId();

        var sut = new InvalidVariantOperationException(id, "تغییر", "غیرفعال است.");

        sut.VariantId.ShouldBe(id);
        sut.Operation.ShouldBe("تغییر");
        sut.Reason.ShouldBe("غیرفعال است.");
    }

    [Fact]
    public void ErrorCode_IsInvalidVariantOperation()
    {
        var sut = new InvalidVariantOperationException(VariantId.NewId(), "op", "reason");

        sut.ErrorCode.ShouldBe("INVALID_VARIANT_OPERATION");
    }

    [Fact]
    public void Message_IncludesVariantIdOperationAndReason()
    {
        var id = VariantId.NewId();

        var sut = new InvalidVariantOperationException(id, "تغییر", "غیرفعال است.");

        sut.Message.ShouldContain(id.ToString());
        sut.Message.ShouldContain("تغییر");
        sut.Message.ShouldContain("غیرفعال است.");
    }

    [Fact]
    public void InheritsFromDomainException()
    {
        new InvalidVariantOperationException(VariantId.NewId(), "op", "reason")
            .ShouldBeAssignableTo<DomainException>();
    }
}
