using Domain.Support.ValueObjects;

namespace Domain.Support;

public class Ticket : AggregateRoot, IAuditable
{
    private readonly List<TicketMessage> _messages = new();

    private string _subject = null!;
    private string _priority;
    private string _status;

    public int UserId { get; private set; }
    public string Subject => _subject;
    public string Priority => _priority;
    public string Status => _status;

    public IReadOnlyCollection<TicketMessage> Messages => _messages.AsReadOnly();

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    
    public Domain.User.User User { get; private set; } = null!;

    
    public bool IsClosed => _status == TicketStatuses.Closed;

    public bool IsOpen => _status == TicketStatuses.Open;
    public bool IsAwaitingReply => _status == TicketStatuses.AwaitingReply;
    public bool IsAnswered => _status == TicketStatuses.Answered;
    public int MessageCount => _messages.Count;
    public DateTime? LastMessageAt => _messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.CreatedAt;

    
    private const int MaxSubjectLength = 200;

    private const int MinSubjectLength = 5;

    public static class TicketStatuses
    {
        public const string Open = "Open";
        public const string AwaitingReply = "AwaitingReply";
        public const string Answered = "Answered";
        public const string Closed = "Closed";
    }

    public static class TicketPriorities
    {
        public const string Low = "Low";
        public const string Normal = "Normal";
        public const string High = "High";
        public const string Urgent = "Urgent";
    }

    private Ticket()
    {
        _priority = TicketPriorities.Normal;
        _status = TicketStatuses.Open;
    }

    #region Factory Methods

    public static Ticket Open(int userId, string subject, string priority, string initialMessage)
    {
        Guard.Against.NegativeOrZero(userId, nameof(userId));
        ValidateSubject(subject);
        ValidatePriority(priority);
        ValidateMessage(initialMessage);

        var ticket = new Ticket
        {
            UserId = userId,
            _subject = subject.Trim(),
            _priority = priority,
            _status = TicketStatuses.Open,
            CreatedAt = DateTime.UtcNow
        };

        var message = TicketMessage.Create(ticket, initialMessage, isAdmin: false);
        ticket._messages.Add(message);

        ticket.AddDomainEvent(new TicketCreatedEvent(ticket.Id, userId));

        return ticket;
    }

    public static Ticket OpenWithDefaultPriority(int userId, string subject, string initialMessage)
    {
        return Open(userId, subject, TicketPriorities.Normal, initialMessage);
    }

    #endregion Factory Methods

    #region Domain Behaviors

    public void AddMessage(string content, bool isAdminReply, int? senderId = null)
    {
        EnsureNotClosed();
        ValidateMessage(content);

        var ticketMessage = TicketMessage.Create(this, content, isAdmin: isAdminReply, senderId: senderId);
        _messages.Add(ticketMessage);

        if (isAdminReply)
        {
            _status = TicketStatuses.Answered;
            AddDomainEvent(new TicketAnsweredEvent(Id, senderId));
        }
        else
        {
            _status = TicketStatuses.AwaitingReply;
        }

        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new TicketMessageAddedEvent(Id, GetMessagePreview(content)));
    }

    public void Close()
    {
        if (IsClosed)
            return;

        _status = TicketStatuses.Closed;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new TicketClosedEvent(Id));
    }

    public void Reopen()
    {
        if (!IsClosed)
            throw new DomainException("تیکت در حال حاضر باز است.");

        _status = TicketStatuses.Open;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new TicketReopenedEvent(Id));
    }

    public void ChangePriority(string newPriority)
    {
        ValidatePriority(newPriority);

        if (_priority == newPriority)
            return;

        var oldPriority = _priority;
        _priority = newPriority;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new TicketPriorityChangedEvent(Id, oldPriority, newPriority));
    }

    public void UpdateSubject(string newSubject)
    {
        EnsureNotClosed();
        ValidateSubject(newSubject);

        _subject = newSubject.Trim();
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new Events.TicketSubjectUpdatedEvent(Id, _subject));
    }

    #endregion Domain Behaviors

    #region Query Methods

    public bool CanAddMessage() => !IsClosed;

    public bool CanClose() => !IsClosed;

    public bool CanReopen() => IsClosed;

    public bool IsHighPriority() =>
        _priority == TicketPriorities.High || _priority == TicketPriorities.Urgent;

    public bool IsUrgent() => _priority == TicketPriorities.Urgent;

    public bool HasAdminResponse() => _messages.Any(m => m.IsAdminResponse);

    public TicketMessage? GetLastAdminResponse() =>
        _messages.Where(m => m.IsAdminResponse).OrderByDescending(m => m.CreatedAt).FirstOrDefault();

    public TicketMessage? GetLastUserMessage() =>
        _messages.Where(m => !m.IsAdminResponse).OrderByDescending(m => m.CreatedAt).FirstOrDefault();

    public TimeSpan? GetTimeSinceCreation() => DateTime.UtcNow - CreatedAt;

    public TimeSpan? GetTimeSinceLastUpdate() => UpdatedAt.HasValue ? DateTime.UtcNow - UpdatedAt.Value : null;

    public bool RequiresUrgentAttention()
    {
        if (IsClosed)
            return false;

        if (IsUrgent())
            return true;

        var timeSinceCreation = GetTimeSinceCreation();
        if (timeSinceCreation.HasValue && IsHighPriority() && timeSinceCreation.Value.TotalHours > 4)
            return true;

        return false;
    }

    #endregion Query Methods

    #region Domain Invariants

    private void EnsureNotClosed()
    {
        if (IsClosed)
            throw new TicketAlreadyClosedException(Id);
    }

    private static void ValidateSubject(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new DomainException("موضوع تیکت الزامی است.");

        var trimmed = subject.Trim();

        if (trimmed.Length < MinSubjectLength)
            throw new DomainException($"موضوع تیکت باید حداقل {MinSubjectLength} کاراکتر باشد.");

        if (trimmed.Length > MaxSubjectLength)
            throw new DomainException($"موضوع تیکت نمی‌تواند بیش از {MaxSubjectLength} کاراکتر باشد.");
    }

    private static void ValidatePriority(string priority)
    {
        TicketPriority.Parse(priority);
    }

    private static void ValidateMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new DomainException("متن پیام الزامی است.");

        if (message.Trim().Length > 5000)
            throw new DomainException("متن پیام نمی‌تواند بیش از ۵۰۰۰ کاراکتر باشد.");
    }

    private static string GetMessagePreview(string message)
    {
        const int maxPreviewLength = 100;
        var trimmed = message.Trim();

        if (trimmed.Length <= maxPreviewLength)
            return trimmed;

        return trimmed.Substring(0, maxPreviewLength) + "...";
    }

    #endregion Domain Invariants
}