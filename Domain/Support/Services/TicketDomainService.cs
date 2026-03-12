namespace Domain.Support.Services;

public sealed class TicketDomainService
{
    private static readonly string[] UrgentKeywords = { "فوری", "اورژانسی", "urgent", "بحرانی", "حیاتی", "critical" };
    private static readonly string[] HighKeywords = { "مهم", "ضروری", "سریع", "important", "asap" };

    public TicketAccessResult ValidateUserAccess(Ticket ticket, UserId userId, bool isAdmin)
    {
        Guard.Against.Null(ticket, nameof(ticket));
        Guard.Against.Null(userId, nameof(userId));

        if (isAdmin)
            return TicketAccessResult.Allowed();

        if (ticket.CustomerId != userId && ticket.AssignedAgentId != userId)
            return TicketAccessResult.Denied("شما دسترسی به این تیکت را ندارید.");

        return TicketAccessResult.Allowed();
    }

    public Result ValidateCanSendMessage(Ticket ticket)
    {
        Guard.Against.Null(ticket, nameof(ticket));

        if (ticket.IsClosed)
            return Result.Failure("تیکت بسته شده است و امکان ارسال پیام وجود ندارد.");

        return Result.Success();
    }

    public Result ValidateCanClose(Ticket ticket)
    {
        Guard.Against.Null(ticket, nameof(ticket));

        if (ticket.IsClosed)
            return Result.Failure("تیکت قبلاً بسته شده است.");

        return Result.Success();
    }

    public Result ValidateCanEditMessage(Ticket ticket, TicketMessageId messageId, UserId editorId, bool isAdmin)
    {
        Guard.Against.Null(ticket, nameof(ticket));
        Guard.Against.Null(messageId, nameof(messageId));
        Guard.Against.Null(editorId, nameof(editorId));

        if (ticket.IsClosed)
            return Result.Failure("امکان ویرایش پیام در تیکت بسته‌شده وجود ندارد.");

        var message = ticket.Messages.FirstOrDefault(m => m.Id == messageId);
        if (message is null)
            return Result.Failure("پیام یافت نشد.");

        if (!isAdmin && message.SenderId != editorId)
            return Result.Failure("شما مجاز به ویرایش این پیام نیستید.");

        var editWindow = TimeSpan.FromMinutes(15);
        if (!isAdmin && DateTime.UtcNow - message.SentAt > editWindow)
            return Result.Failure("مهلت ویرایش پیام به پایان رسیده است.");

        return Result.Success();
    }

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
        var unassigned = ticketList.Count(t => t.AssignedAgentId is null && !t.IsClosed);

        var avgResponseTime = CalculateAverageResponseTime(ticketList);

        var priorityBreakdown = ticketList
            .GroupBy(t => t.Priority.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var statusBreakdown = ticketList
            .GroupBy(t => t.Status.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        return new TicketStatistics(
            total,
            open,
            awaitingReply,
            answered,
            closed,
            highPriority,
            urgent,
            unassigned,
            avgResponseTime,
            priorityBreakdown,
            statusBreakdown);
    }

    public TicketPriority SuggestPriority(string subject, string message)
    {
        Guard.Against.NullOrWhiteSpace(subject, nameof(subject));

        var combinedText = $"{subject} {message ?? string.Empty}";

        if (UrgentKeywords.Any(k => combinedText.Contains(k, StringComparison.OrdinalIgnoreCase)))
            return TicketPriority.Urgent;

        if (HighKeywords.Any(k => combinedText.Contains(k, StringComparison.OrdinalIgnoreCase)))
            return TicketPriority.High;

        return TicketPriority.Normal;
    }

    public IEnumerable<Ticket> FilterRequiringUrgentAttention(IEnumerable<Ticket> tickets)
    {
        Guard.Against.Null(tickets, nameof(tickets));

        return tickets.Where(t => t.RequiresUrgentAttention());
    }

    public IEnumerable<Ticket> SortByPriorityAndAge(IEnumerable<Ticket> tickets)
    {
        Guard.Against.Null(tickets, nameof(tickets));

        return tickets
            .OrderByDescending(t => t.Priority.SortOrder)
            .ThenBy(t => t.CreatedAt);
    }

    public IEnumerable<Ticket> GetOverdueTickets(IEnumerable<Ticket> tickets, TimeSpan overdueThreshold)
    {
        Guard.Against.Null(tickets, nameof(tickets));

        var cutoff = DateTime.UtcNow - overdueThreshold;

        return tickets.Where(t =>
            !t.IsClosed &&
            t.IsAwaitingReply &&
            (t.LastActivityAt ?? t.CreatedAt) < cutoff);
    }

    public IEnumerable<Ticket> GetTicketsWithoutResponse(IEnumerable<Ticket> tickets, TimeSpan responseThreshold)
    {
        Guard.Against.Null(tickets, nameof(tickets));

        var cutoff = DateTime.UtcNow - responseThreshold;

        return tickets.Where(t =>
            !t.IsClosed &&
            t.IsOpen &&
            t.Messages.All(m => m.SenderType != TicketMessageSenderType.Agent) &&
            t.CreatedAt < cutoff);
    }

    public Result ValidatePriorityChange(Ticket ticket, ValueObjects.TicketPriority newPriority, bool isAdmin)
    {
        Guard.Against.Null(ticket, nameof(ticket));
        Guard.Against.Null(newPriority, nameof(newPriority));

        if (ticket.IsClosed)
            return Result.Failure("امکان تغییر اولویت تیکت بسته‌شده وجود ندارد.");

        if (!isAdmin && newPriority == ValueObjects.TicketPriority.Urgent)
            return Result.Failure("فقط مدیران می‌توانند اولویت تیکت را به فوری تغییر دهند.");

        if (ticket.Priority == newPriority)
            return Result.Failure("اولویت جدید با اولویت فعلی یکسان است.");

        return Result.Success();
    }

    public Result ValidateAssignment(Ticket ticket, UserId agentId)
    {
        Guard.Against.Null(ticket, nameof(ticket));
        Guard.Against.Null(agentId, nameof(agentId));

        if (ticket.IsClosed)
            return Result.Failure("امکان تخصیص تیکت بسته‌شده وجود ندارد.");

        if (ticket.AssignedAgentId == agentId)
            return Result.Failure("این تیکت قبلاً به این اپراتور تخصیص داده شده است.");

        return Result.Success();
    }

    private static TimeSpan? CalculateAverageResponseTime(IReadOnlyList<Ticket> tickets)
    {
        var responseTimes = tickets
            .Select(t => t.GetTimeToFirstResponse())
            .Where(t => t.HasValue)
            .Select(t => t!.Value)
            .ToList();

        if (!responseTimes.Any()) return null;

        var totalTicks = responseTimes.Sum(t => t.Ticks);
        return TimeSpan.FromTicks(totalTicks / responseTimes.Count);
    }
}

public sealed record TicketStatistics(
    int TotalCount,
    int OpenCount,
    int AwaitingReplyCount,
    int AnsweredCount,
    int ClosedCount,
    int HighPriorityCount,
    int UrgentCount,
    int UnassignedCount,
    TimeSpan? AverageResponseTime,
    Dictionary<string, int> PriorityBreakdown,
    Dictionary<string, int> StatusBreakdown)
{
    public decimal OpenPercentage =>
        TotalCount > 0 ? Math.Round((decimal)OpenCount / TotalCount * 100, 2) : 0;

    public decimal ClosedPercentage =>
        TotalCount > 0 ? Math.Round((decimal)ClosedCount / TotalCount * 100, 2) : 0;

    public decimal AwaitingReplyPercentage =>
        TotalCount > 0 ? Math.Round((decimal)AwaitingReplyCount / TotalCount * 100, 2) : 0;

    public decimal ResolutionRate =>
        TotalCount > 0 ? Math.Round((decimal)(ClosedCount + AnsweredCount) / TotalCount * 100, 2) : 0;

    public bool HasUrgentTickets => UrgentCount > 0;

    public bool HasHighPriorityTickets => HighPriorityCount > 0;

    public bool HasUnassignedTickets => UnassignedCount > 0;

    public string GetAverageResponseTimeDisplay()
    {
        if (!AverageResponseTime.HasValue) return "نامشخص";

        var ts = AverageResponseTime.Value;

        if (ts.TotalMinutes < 60)
            return $"{(int)ts.TotalMinutes} دقیقه";

        if (ts.TotalHours < 24)
            return $"{(int)ts.TotalHours} ساعت";

        return $"{(int)ts.TotalDays} روز";
    }
}