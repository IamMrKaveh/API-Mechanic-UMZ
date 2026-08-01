using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.SharedKernel.ValueObjects;

public class FileSizeTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1024)]
    [InlineData(100L * 1024 * 1024)]
    public void Create_WithNonNegativeValueUnderCap_ReturnsFileSize(long bytes)
    {
        FileSize.Create(bytes).Bytes.ShouldBe(bytes);
    }

    [Fact]
    public void Create_WithNegativeValue_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => FileSize.Create(-1));
    }

    [Fact]
    public void Create_WithValueExceedingAbsoluteMax_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => FileSize.Create(FileSize.AbsoluteMaxBytes + 1));
    }

    [Fact]
    public void AbsoluteMaxBytes_Is100Megabytes()
    {
        FileSize.AbsoluteMaxBytes.ShouldBe(100L * 1024 * 1024);
    }

    [Fact]
    public void From_WithBytesUnderBothCaps_ReturnsFileSize()
    {
        FileSize.From(1024, 2048).Bytes.ShouldBe(1024L);
    }

    [Fact]
    public void From_WithNegativeBytes_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => FileSize.From(-1, 1024));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void From_WithNonPositiveMaxAllowed_ThrowsDomainException(long maxAllowed)
    {
        Should.Throw<DomainException>(() => FileSize.From(1, maxAllowed));
    }

    [Fact]
    public void From_WhenBytesExceedCallerMaxButUnderAbsoluteMax_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => FileSize.From(2048, 1024));
    }

    [Fact]
    public void From_WhenCallerMaxAboveAbsoluteMax_StillCapsAtAbsoluteMax()
    {
        var underAbsolute = FileSize.AbsoluteMaxBytes;

        Should.NotThrow(() => FileSize.From(underAbsolute, long.MaxValue));
        Should.Throw<DomainException>(() => FileSize.From(underAbsolute + 1, long.MaxValue));
    }

    [Fact]
    public void FromKilobytes_ProducesExpectedByteCount()
    {
        FileSize.FromKilobytes(2).Bytes.ShouldBe(2 * 1024L);
    }

    [Fact]
    public void FromMegabytes_ProducesExpectedByteCount()
    {
        FileSize.FromMegabytes(3).Bytes.ShouldBe(3L * 1024 * 1024);
    }

    [Fact]
    public void Zero_HasZeroBytesAndIsEmpty()
    {
        var sut = FileSize.Zero();

        sut.Bytes.ShouldBe(0);
        sut.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void ToKilobytes_ReturnsBytesDividedBy1024()
    {
        FileSize.Create(2048).ToKilobytes().ShouldBe(2.0);
    }

    [Fact]
    public void ToMegabytes_ReturnsBytesDividedBy1024Squared()
    {
        FileSize.FromMegabytes(3).ToMegabytes().ShouldBe(3.0);
    }

    [Fact]
    public void ToDisplayString_WithBytesRange_UsesBytesLabel()
    {
        FileSize.Create(500).ToDisplayString().ShouldContain("بایت");
    }

    [Fact]
    public void ToDisplayString_WithKilobytesRange_UsesKilobyteLabel()
    {
        FileSize.FromKilobytes(5).ToDisplayString().ShouldContain("کیلوبایت");
    }

    [Fact]
    public void ToDisplayString_WithMegabytesRange_UsesMegabyteLabel()
    {
        FileSize.FromMegabytes(5).ToDisplayString().ShouldContain("مگابایت");
    }

    [Fact]
    public void ToString_DelegatesToDisplayString()
    {
        var sut = FileSize.FromKilobytes(5);

        sut.ToString().ShouldBe(sut.ToDisplayString());
    }

    [Fact]
    public void IsEmpty_OnPositiveByteCount_ReturnsFalse()
    {
        FileSize.Create(1).IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public void CompareTo_LargerVsSmaller_ReturnsPositive()
    {
        FileSize.Create(2048).CompareTo(FileSize.Create(1024)).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_EqualValues_ReturnsZero()
    {
        FileSize.Create(1024).CompareTo(FileSize.Create(1024)).ShouldBe(0);
    }

    [Fact]
    public void CompareTo_Null_ReturnsPositive()
    {
        FileSize.Create(1).CompareTo(null).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GreaterThanOperator_LhsBigger_ReturnsTrue()
    {
        (FileSize.Create(2048) > FileSize.Create(1024)).ShouldBeTrue();
    }

    [Fact]
    public void LessThanOperator_LhsSmaller_ReturnsTrue()
    {
        (FileSize.Create(1024) < FileSize.Create(2048)).ShouldBeTrue();
    }

    [Fact]
    public void GreaterThanOrEqualOperator_LhsEqual_ReturnsTrue()
    {
        (FileSize.Create(1024) >= FileSize.Create(1024)).ShouldBeTrue();
    }

    [Fact]
    public void LessThanOrEqualOperator_LhsEqual_ReturnsTrue()
    {
        (FileSize.Create(1024) <= FileSize.Create(1024)).ShouldBeTrue();
    }

    [Fact]
    public void ImplicitConversion_ToLong_ReturnsBytes()
    {
        long b = FileSize.Create(2048);

        b.ShouldBe(2048L);
    }

    [Fact]
    public void Equality_ForValueObjectWithSameBytes_TreatsInstancesAsEqual()
    {
        FileSize.Create(1024).ShouldBe(FileSize.Create(1024));
    }
}
