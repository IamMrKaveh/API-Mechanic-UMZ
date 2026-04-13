namespace SharedKernel.Abstractions.Interfaces;

public interface IBusinessRule
{
    bool IsBroken();

    string Message { get; }
}