namespace Domain.Common.Exceptions;

public class InvalidEntityStateException : DomainException
{
    public string EntityName { get; }
    public object EntityId { get; }
    public string CurrentState { get; }
    public string ExpectedState { get; }

    public InvalidEntityStateException(
        string entityName,
        object entityId,
        string currentState,
        string expectedState)
        : base($"{entityName} با شناسه {entityId} در وضعیت {currentState} است اما باید در وضعیت {expectedState} باشد.")
    {
        EntityName = entityName;
        EntityId = entityId;
        CurrentState = currentState;
        ExpectedState = expectedState;
    }
}