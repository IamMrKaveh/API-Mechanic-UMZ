namespace SharedKernel.ValueObjects;

public interface IStronglyTypedId
{
    Guid Value { get; }
}