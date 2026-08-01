using SharedKernel.Abstractions;

namespace Tests.SharedKernel.Abstractions;

public class ValueObjectTests
{
    private sealed class TestVo : ValueObject
    {
        public string A { get; }
        public int B { get; }

        public TestVo(string a, int b)
        { A = a; B = b; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return A;
            yield return B;
        }
    }

    private sealed class OtherVo : ValueObject
    {
        public string A { get; }

        public OtherVo(string a)
        { A = a; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return A;
        }
    }

    [Fact]
    public void Equals_ForInstancesWithSameComponents_ReturnsTrue()
    {
        var a = new TestVo("x", 1);
        var b = new TestVo("x", 1);

        a.Equals(b).ShouldBeTrue();
        (a == b).ShouldBeTrue();
        (a != b).ShouldBeFalse();
    }

    [Fact]
    public void Equals_ForInstancesWithDifferentComponents_ReturnsFalse()
    {
        var a = new TestVo("x", 1);
        var b = new TestVo("x", 2);

        a.Equals(b).ShouldBeFalse();
        (a == b).ShouldBeFalse();
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void Equals_AgainstNull_ReturnsFalse()
    {
        var a = new TestVo("x", 1);

        a.Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void Equals_AgainstDifferentType_ReturnsFalseEvenWhenComponentsOverlap()
    {
        var a = new TestVo("x", 0);
        var b = new OtherVo("x");

        a.Equals(b).ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_ForEqualInstances_ReturnsSameHash()
    {
        var a = new TestVo("x", 1);
        var b = new TestVo("x", 1);

        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        TestVo? a = null;
        TestVo? b = null;

        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void EqualityOperator_OneNull_ReturnsFalse()
    {
        var a = new TestVo("x", 1);

        (a == null).ShouldBeFalse();
        (null == a).ShouldBeFalse();
    }
}
