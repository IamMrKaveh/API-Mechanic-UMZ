namespace Domain.Support.Exceptions;

public sealed class TicketAlreadyClosedException : DomainException
{
    public int TicketId { get; }

    public TicketAlreadyClosedException(int ticketId)
        : base($"تیکت {ticketId} قبلاً بسته شده است و امکان ارسال پیام جدید وجود ندارد.")
    {
        TicketId = ticketId;
    }

    public TicketAlreadyClosedException()
        : base("��یکت قبلاً بسته شده است و امکان ارسال پیام جدید وجود ندارد.")
    {
        TicketId = 0;
    }
}