using SharedKernel.Abstractions;

namespace Tests.SharedKernel.Abstractions;

public class EntityTests
{
    private sealed class TestEntity : Entity<Guid>
    {
        public TestEntity()
        { }

        public TestEntity(Guid id) : base(id)
        { }
    }

    private sealed class OtherEntity : Entity<Guid>
    {
        public OtherEntity(Guid id) : base(id)
        { }
    }

    private sealed class IntEntity : Entity<int>
    {
        public IntEntity(int id) : base(id)
        { }
    }

    [Fact]
    public void Constructor_WithId_SetsId()
    {
        var id = Guid.NewGuid();

        new TestEntity(id).Id.ShouldBe(id);
    }

    [Fact]
    public void Constructor_Parameterless_LeavesDefaultId()
    {
        new TestEntity().Id.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var entity = new TestEntity(Guid.NewGuid());

        entity.Equals(entity).ShouldBeTrue();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        new TestEntity(Guid.NewGuid()).Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void Equals_NonEntity_ReturnsFalse()
    {
        new TestEntity(Guid.NewGuid()).Equals("not-an-entity").ShouldBeFalse();
    }

    [Fact]
    public void Equals_SameTypeAndSameId_ReturnsTrue()
    {
        var id = Guid.NewGuid();

        new TestEntity(id).Equals(new TestEntity(id)).ShouldBeTrue();
    }

    [Fact]
    public void Equals_SameTypeDifferentId_ReturnsFalse()
    {
        new TestEntity(Guid.NewGuid()).Equals(new TestEntity(Guid.NewGuid())).ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentTypesSameId_ReturnsFalse()
    {
        var id = Guid.NewGuid();

        new TestEntity(id).Equals(new OtherEntity(id)).ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_EqualEntities_ReturnSameHash()
    {
        var id = Guid.NewGuid();

        new TestEntity(id).GetHashCode().ShouldBe(new TestEntity(id).GetHashCode());
    }

    [Fact]
    public void Entities_WorkAsDictionaryKeysById()
    {
        var id = Guid.NewGuid();
        var dict = new Dictionary<TestEntity, string>
        {
            [new TestEntity(id)] = "value"
        };

        dict[new TestEntity(id)].ShouldBe("value");
        dict.ContainsKey(new TestEntity(Guid.NewGuid())).ShouldBeFalse();
    }

    [Fact]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        TestEntity? left = null;
        TestEntity? right = null;

        (left == right).ShouldBeTrue();
        (left != right).ShouldBeFalse();
    }

    [Fact]
    public void EqualityOperator_OneNull_ReturnsFalse()
    {
        var entity = new TestEntity(Guid.NewGuid());
        TestEntity? nullEntity = null;

        (entity == nullEntity).ShouldBeFalse();
        (nullEntity == entity).ShouldBeFalse();
        (entity != nullEntity).ShouldBeTrue();
    }

    [Fact]
    public void EqualityOperator_SameId_ReturnsTrue()
    {
        var id = Guid.NewGuid();

        (new TestEntity(id) == new TestEntity(id)).ShouldBeTrue();
        (new TestEntity(id) != new TestEntity(id)).ShouldBeFalse();
    }

    [Fact]
    public void EqualityOperator_DifferentId_ReturnsFalse()
    {
        (new TestEntity(Guid.NewGuid()) == new TestEntity(Guid.NewGuid())).ShouldBeFalse();
    }

    [Fact]
    public void Equality_SupportsNonGuidKeys()
    {
        new IntEntity(5).Equals(new IntEntity(5)).ShouldBeTrue();
        new IntEntity(5).Equals(new IntEntity(6)).ShouldBeFalse();
    }
}
