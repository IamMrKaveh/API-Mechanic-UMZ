using Domain.Support.Aggregates;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class TicketBuilder
{
    private static readonly Faker Faker = new();

    private TicketId _id = TicketId.NewId();
    private UserId _customerId = UserId.NewId();
    private string _subject = Faker.Lorem.Sentence(wordCount: 4).TrimEnd('.');
    private TicketCategory _category = new TicketCategoryBuilder().Build();
    private TicketPriority? _priority;

    public TicketBuilder WithId(TicketId id)
    {
        _id = id;
        return this;
    }

    public TicketBuilder WithCustomerId(UserId customerId)
    {
        _customerId = customerId;
        return this;
    }

    public TicketBuilder WithSubject(string subject)
    {
        _subject = subject;
        return this;
    }

    public TicketBuilder WithCategory(TicketCategory category)
    {
        _category = category;
        return this;
    }

    public TicketBuilder WithCategoryValue(string value)
    {
        _category = new TicketCategoryBuilder().WithValue(value).Build();
        return this;
    }

    public TicketBuilder WithPriority(TicketPriority? priority)
    {
        _priority = priority;
        return this;
    }

    public Ticket Build() => Ticket.Open(_id, _customerId, _subject, _category, _priority);
}
