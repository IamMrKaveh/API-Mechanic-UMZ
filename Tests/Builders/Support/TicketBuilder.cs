using Domain.Support.Aggregates;

namespace Tests.Builders.Support;

public class TicketBuilder
{
    private int _userId = 1;
    private string _subject = "مشکل در سفارش";
    private string _priority = Ticket.TicketPriorities.Normal;
    private string _message = "لطفاً وضعیت سفارش من را بررسی کنید.";

    public TicketBuilder WithUserId(int userId)
    {
        _userId = userId;
        return this;
    }

    public TicketBuilder WithSubject(string subject)
    {
        _subject = subject;
        return this;
    }

    public TicketBuilder WithPriority(string priority)
    {
        _priority = priority;
        return this;
    }

    public TicketBuilder WithMessage(string message)
    {
        _message = message;
        return this;
    }

    public TicketBuilder AsUrgent()
    {
        _priority = Ticket.TicketPriorities.Urgent;
        return this;
    }

    public TicketBuilder AsHighPriority()
    {
        _priority = Ticket.TicketPriorities.High;
        return this;
    }

    public Ticket Build()
    {
        return Ticket.Open(_userId, _subject, _priority, _message);
    }

    public Ticket BuildClosed()
    {
        var ticket = Build();
        ticket.Close();
        return ticket;
    }
}