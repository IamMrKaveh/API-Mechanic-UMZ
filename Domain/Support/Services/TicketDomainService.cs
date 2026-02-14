namespace Domain.Support.Services;

/// <summary>
/// Domain Service برای عملیات‌های پیچیده تیکت
/// Stateless - بدون وابستگی به Infrastructure
/// </summary>
public sealed class TicketDomainService
{
    /// <summary>
    /// اعتبارسنجی دسترسی کاربر به تیکت
    /// </summary>
    public (bool HasAccess, string? Error) ValidateUserAccess(Ticket ticket, int userId, bool isAdmin)
    {
        Guard.Against.Null(ticket, nameof(ticket));

        if (isAdmin)
            return (true, null);

        if (ticket.UserId != userId)
            return (false, "شما دسترسی به این تیکت را ندارید.");

        return (true, null);
    }

    /// <summary>
    /// اعتبارسنجی امکان ارسال پیام
    /// </summary>
    public (bool CanSend, string? Error) ValidateCanSendMessage(Ticket ticket)
    {
        Guard.Against.Null(ticket, nameof(ticket));

        if (ticket.IsClosed)
            return (false, "تیکت بسته شده است و امکان ارسال پیام وجود ندارد.");

        return (true, null);
    }

    /// <summary>
    /// اعتبارسنجی امکان بستن تیکت
    /// </summary>
    public (bool CanClose, string? Error) ValidateCanClose(Ticket ticket)
    {
        Guard.Against.Null(ticket, nameof(ticket));

        if (ticket.IsClosed)
            return (false, "تیکت قبلاً بسته شده است.");

        return (true, null);
    }

    /// <summary>
    /// محاسبه آمار تیکت‌ها
    /// </summary>
    public TicketStatistics CalculateStatistics(IEnumerable<Ticket> tickets)
    {
        Guard.Against.Null(tickets, nameof(tickets));

        var ticketList = tickets.ToList();

        var total = ticketList.Count;
        var open = ticketList.Count(t => t.IsOpen);
        var awaitingReply = ticketList.Count(t => t.IsAwaitingReply);
        var answered = ticketList.Count(t => t.IsAnswered);
        var closed = ticketList.Count(t => t.IsClosed);
        var highPriority = ticketList.Count(t => t.IsHighPriority() && !t.IsClosed);
        var urgent = ticketList.Count(t => t.IsUrgent() && !t.IsClosed);

        var priorityBreakdown = ticketList
            .GroupBy(t => t.Priority)
            .ToDictionary(g => g.Key, g => g.Count());

        return new TicketStatistics(
            total,
            open,
            awaitingReply,
            answered,
            closed,
            highPriority,
            urgent,
            priorityBreakdown);
    }

    /// <summary>
    /// تعیین اولویت پیشنهادی بر اساس محتوای تیکت
    /// </summary>
    public string SuggestPriority(string subject, string message)
    {
        var urgentKeywords = new[] { "فوری", "اورژانسی", "urgent", "بحرانی", "حیاتی" };
        var highKeywords = new[] { "مهم", "ضروری", "سریع", "important" };

        var combinedText = $"{subject} {message}".ToLowerInvariant();

        if (urgentKeywords.Any(k => combinedText.Contains(k, StringComparison.OrdinalIgnoreCase)))
            return Ticket.TicketPriorities.Urgent;

        if (highKeywords.Any(k => combinedText.Contains(k, StringComparison.OrdinalIgnoreCase)))
            return Ticket.TicketPriorities.High;

        return Ticket.TicketPriorities.Normal;
    }

    /// <summary>
    /// فیلتر تیکت‌های نیازمند توجه فوری
    /// </summary>
    public IEnumerable<Ticket> FilterRequiringUrgentAttention(IEnumerable<Ticket> tickets)
    {
        Guard.Against.Null(tickets, nameof(tickets));

        return tickets.Where(t => t.RequiresUrgentAttention());
    }

    /// <summary>
    /// مرتب‌سازی تیکت‌ها بر اساس اولویت و زمان
    /// </summary>
    public IEnumerable<Ticket> SortByPriorityAndAge(IEnumerable<Ticket> tickets)
    {
        Guard.Against.Null(tickets, nameof(tickets));

        var priorityOrder = new Dictionary<string, int>
        {
            { Ticket.TicketPriorities.Urgent, 0 },
            { Ticket.TicketPriorities.High, 1 },
            { Ticket.TicketPriorities.Normal, 2 },
            { Ticket.TicketPriorities.Low, 3 }
        };

        return tickets
            .OrderBy(t => priorityOrder.TryGetValue(t.Priority, out var order) ? order : 99)
            .ThenBy(t => t.CreatedAt);
    }
}

/// <summary>
/// آمار تیکت‌ها
/// </summary>
public sealed record TicketStatistics(
    int TotalCount,
    int OpenCount,
    int AwaitingReplyCount,
    int AnsweredCount,
    int ClosedCount,
    int HighPriorityCount,
    int UrgentCount,
    Dictionary<string, int> PriorityBreakdown)
{
    public decimal OpenPercentage =>
        TotalCount > 0 ? Math.Round((decimal)OpenCount / TotalCount * 100, 2) : 0;

    public decimal ClosedPercentage =>
        TotalCount > 0 ? Math.Round((decimal)ClosedCount / TotalCount * 100, 2) : 0;

    public decimal AwaitingReplyPercentage =>
        TotalCount > 0 ? Math.Round((decimal)AwaitingReplyCount / TotalCount * 100, 2) : 0;

    public bool HasUrgentTickets => UrgentCount > 0;

    public bool HasHighPriorityTickets => HighPriorityCount > 0;
}