using Domain.Variant.Exceptions;
using Domain.Variant.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Variant.Exceptions;

public class VariantExceptionsTests
{
    [Fact]
    public void InvalidVariantOperationException_ExposesAllArgumentsAndErrorCode()
    {
        var variantId = VariantId.NewId();

        var sut = new InvalidVariantOperationException(variantId, "Activate", "Already active");

        sut.VariantId.ShouldBe(variantId);
        sut.Operation.ShouldBe("Activate");
        sut.Reason.ShouldBe("Already active");
        sut.ErrorCode.ShouldBe("INVALID_VARIANT_OPERATION");
        sut.Message.ShouldContain("Activate");
        sut.Message.ShouldContain("Already active");
        sut.ShouldBeAssignableTo<DomainException>();
    }
}
