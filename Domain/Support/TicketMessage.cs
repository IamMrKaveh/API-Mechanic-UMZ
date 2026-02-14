namespace Domain.Support;

public class TicketMessage : BaseEntity, IAuditable
{
    private string _message = null!;

    public int TicketId { get; private set; }
    public int? SenderId { get; private set; }
    public string Message => _message;
    public bool IsAdminResponse { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    //Navigation
    public Ticket Ticket { get; private set; }

    // Business Constants
    private const int MaxMessageLength = 5000;

    private const int MinMessageLength = 1;

    private TicketMessage()
    { }

    #region Factory Method

    internal static TicketMessage Create(Ticket ticket, string message, bool isAdmin, int? senderId = null)
    {
        Guard.Against.Null(ticket, nameof(ticket));
        ValidateMessage(message);

        if (isAdmin && !senderId.HasValue)
        {
            // برای پیام‌های ادمین، senderId توصیه می‌شود اما اجباری نیست
        }

        return new TicketMessage
        {
            TicketId = ticket.Id,
            _message = message.Trim(),
            IsAdminResponse = isAdmin,
            SenderId = senderId,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion Factory Method

    #region Domain Behaviors

    internal void UpdateContent(string newMessage)
    {
        ValidateMessage(newMessage);

        _message = newMessage.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Domain Behaviors

    #region Query Methods

    public bool IsUserMessage() => !IsAdminResponse;

    public string GetPreview(int maxLength = 100)
    {
        if (_message.Length <= maxLength)
            return _message;

        return _message.Substring(0, maxLength) + "...";
    }

    public TimeSpan GetTimeSinceCreation() => DateTime.UtcNow - CreatedAt;

    public bool IsRecent(TimeSpan threshold) => GetTimeSinceCreation() <= threshold;

    #endregion Query Methods

    #region Validation

    private static void ValidateMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new DomainException("متن پیام الزامی است.");

        var trimmed = message.Trim();

        if (trimmed.Length < MinMessageLength)
            throw new DomainException("متن پیام نمی‌تواند خالی باشد.");

        if (trimmed.Length > MaxMessageLength)
            throw new DomainException($"متن پیام نمی‌تواند بیش از {MaxMessageLength} کاراکتر باشد.");
    }

    #endregion Validation
}