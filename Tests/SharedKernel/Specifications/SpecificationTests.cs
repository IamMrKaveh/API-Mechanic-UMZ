using System.Linq.Expressions;
using SharedKernel.Specifications;

namespace Tests.SharedKernel.Specifications;

public class SpecificationTests
{
    private sealed class Person
    {
        public string Name { get; init; } = string.Empty;
        public int Age { get; init; }
        public bool IsActive { get; init; }
    }

    private sealed class AdultSpec : Specification<Person>
    {
        public override Expression<Func<Person, bool>> ToExpression() => p => p.Age >= 18;
    }

    private sealed class ActiveSpec : Specification<Person>
    {
        public override Expression<Func<Person, bool>> ToExpression() => p => p.IsActive;
    }

    private sealed class NameStartsWithASpec : Specification<Person>
    {
        public override Expression<Func<Person, bool>> ToExpression() =>
            p => p.Name.StartsWith("A");
    }

    [Fact]
    public void IsSatisfiedBy_WhenPredicateHolds_ReturnsTrue()
    {
        new AdultSpec().IsSatisfiedBy(new Person { Age = 20 }).ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_WhenPredicateDoesNotHold_ReturnsFalse()
    {
        new AdultSpec().IsSatisfiedBy(new Person { Age = 17 }).ShouldBeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_UsesCachedCompiledPredicateAcrossMultipleCalls()
    {
        var spec = new AdultSpec();

        spec.IsSatisfiedBy(new Person { Age = 18 }).ShouldBeTrue();
        spec.IsSatisfiedBy(new Person { Age = 17 }).ShouldBeFalse();
        spec.IsSatisfiedBy(new Person { Age = 99 }).ShouldBeTrue();
    }

    [Fact]
    public void AndOperator_WhenBothSpecificationsHold_ReturnsTrue()
    {
        var combined = new AdultSpec() & new ActiveSpec();

        combined.IsSatisfiedBy(new Person { Age = 30, IsActive = true }).ShouldBeTrue();
    }

    [Fact]
    public void AndOperator_WhenOneSpecificationFails_ReturnsFalse()
    {
        var combined = new AdultSpec() & new ActiveSpec();

        combined.IsSatisfiedBy(new Person { Age = 30, IsActive = false }).ShouldBeFalse();
        combined.IsSatisfiedBy(new Person { Age = 10, IsActive = true }).ShouldBeFalse();
    }

    [Fact]
    public void OrOperator_WhenEitherSpecificationHolds_ReturnsTrue()
    {
        var combined = new AdultSpec() | new ActiveSpec();

        combined.IsSatisfiedBy(new Person { Age = 30, IsActive = false }).ShouldBeTrue();
        combined.IsSatisfiedBy(new Person { Age = 10, IsActive = true }).ShouldBeTrue();
    }

    [Fact]
    public void OrOperator_WhenNeitherSpecificationHolds_ReturnsFalse()
    {
        var combined = new AdultSpec() | new ActiveSpec();

        combined.IsSatisfiedBy(new Person { Age = 10, IsActive = false }).ShouldBeFalse();
    }

    [Fact]
    public void NotOperator_InvertsPredicate()
    {
        var negated = !new AdultSpec();

        negated.IsSatisfiedBy(new Person { Age = 10 }).ShouldBeTrue();
        negated.IsSatisfiedBy(new Person { Age = 30 }).ShouldBeFalse();
    }

    [Fact]
    public void CombinedSpecification_ChainedThreeWithAndOrNot_EvaluatesCorrectly()
    {
        var spec = (new AdultSpec() & new ActiveSpec()) & !new NameStartsWithASpec();

        spec.IsSatisfiedBy(new Person { Name = "Bob", Age = 30, IsActive = true }).ShouldBeTrue();
        spec.IsSatisfiedBy(new Person { Name = "Alice", Age = 30, IsActive = true }).ShouldBeFalse();
        spec.IsSatisfiedBy(new Person { Name = "Bob", Age = 10, IsActive = true }).ShouldBeFalse();
    }

    [Fact]
    public void ToExpression_OfCombinedSpec_ProducesQueryableThatFiltersCorrectly()
    {
        var people = new[]
        {
            new Person { Name = "Alice", Age = 30, IsActive = true },
            new Person { Name = "Bob",   Age = 10, IsActive = true },
            new Person { Name = "Cara",  Age = 40, IsActive = false },
            new Person { Name = "Dan",   Age = 25, IsActive = true }
        };

        var spec = new AdultSpec() & new ActiveSpec();

        var filtered = people.AsQueryable().Where(spec.ToExpression()).ToArray();

        filtered.Length.ShouldBe(2);
        filtered.Select(p => p.Name).ShouldBe(new[] { "Alice", "Dan" });
    }

    [Fact]
    public void ToExpression_OfNotSpec_ProducesQueryableThatFiltersCorrectly()
    {
        var people = new[]
        {
            new Person { Name = "Alice", Age = 30 },
            new Person { Name = "Bob",   Age = 10 }
        };

        var spec = !new AdultSpec();

        var filtered = people.AsQueryable().Where(spec.ToExpression()).ToArray();

        filtered.Length.ShouldBe(1);
        filtered[0].Name.ShouldBe("Bob");
    }

    [Fact]
    public void ToExpression_OfOrSpec_ProducesQueryableThatFiltersCorrectly()
    {
        var people = new[]
        {
            new Person { Name = "Alice", Age = 30, IsActive = false },
            new Person { Name = "Bob",   Age = 10, IsActive = true  },
            new Person { Name = "Cara",  Age = 10, IsActive = false }
        };

        var spec = new AdultSpec() | new ActiveSpec();

        var filtered = people.AsQueryable().Where(spec.ToExpression()).ToArray();

        filtered.Length.ShouldBe(2);
    }
}
