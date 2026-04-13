namespace SharedKernel.Exceptions;

public sealed class InvalidEntityStateException : DomainException
{
    public string EntityName { get; }
    public string EntityId { get; }
    public string CurrentState { get; }
    public string ExpectedState { get; }

    public override string ErrorCode => "INVALID_ENTITY_STATE";

    public InvalidEntityStateException(
        string entityName,
        string entityId,
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