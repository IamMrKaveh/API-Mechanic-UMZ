using Domain.Support.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class TicketCategoryBuilder
{
    private static readonly Faker Faker = new();

    private string _value = Faker.PickRandom("Billing", "Technical", "Account", "General");

    public TicketCategoryBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }

    public TicketCategory Build() => TicketCategory.Create(_value);
}
