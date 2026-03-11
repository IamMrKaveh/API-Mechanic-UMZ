namespace Domain.Common.Exceptions;

public class InvalidEntityStateException(
    string entityName,
    object entityId,
    string currentState,
    string expectedState) : DomainException($"{entityName} با شناسه {entityId} در وضعیت {currentState} است اما باید در وضعیت {expectedState} باشد.")
{
    public string EntityName { get; } = entityName;
    public object EntityId { get; } = entityId;
    public string CurrentState { get; } = currentState;
    public string ExpectedState { get; } = expectedState;
}