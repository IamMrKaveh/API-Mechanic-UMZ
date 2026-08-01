using SharedKernel.Results;

namespace Tests.SharedKernel.Results;

public class ValidationErrorTests
{
    [Fact]
    public void Constructor_WithRequiredMembersOnly_SetsPropertiesAndLeavesOptionalsNull()
    {
        var sut = new ValidationError("Email", "Email is invalid.");

        sut.Property.ShouldBe("Email");
        sut.Message.ShouldBe("Email is invalid.");
        sut.Code.ShouldBeNull();
        sut.AttemptedValue.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithAllMembers_SetsAllProperties()
    {
        var sut = new ValidationError("Age", "Must be positive.", "GEN_VALIDATION", -3);

        sut.Property.ShouldBe("Age");
        sut.Message.ShouldBe("Must be positive.");
        sut.Code.ShouldBe("GEN_VALIDATION");
        sut.AttemptedValue.ShouldBe(-3);
    }

    [Fact]
    public void Equality_ForRecordWithSameMembers_TreatsInstancesAsEqual()
    {
        var a = new ValidationError("Email", "Invalid.", "CODE", "x");
        var b = new ValidationError("Email", "Invalid.", "CODE", "x");

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void Equality_ForRecordWithDifferentProperty_TreatsInstancesAsUnequal()
    {
        var a = new ValidationError("Email", "Invalid.");
        var b = new ValidationError("Phone", "Invalid.");

        a.ShouldNotBe(b);
    }
}
