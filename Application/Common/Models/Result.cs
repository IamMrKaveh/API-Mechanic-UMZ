namespace Application.Common.Models;

public class Result
{
    public bool Succeeded { get; protected set; }
    public string? Error { get; protected set; }
    public bool IsConcurrencyError { get; protected set; }

    public static Result Ok() => new() { Succeeded = true };
    public static Result Fail(string error, bool isConcurrencyError = false) => new() { Succeeded = false, Error = error, IsConcurrencyError = isConcurrencyError };

    public static Result<T> Ok<T>(T data) => new() { Succeeded = true, Data = data };
    public static Result<T> Fail<T>(string error, bool isConcurrencyError = false) => new() { Succeeded = false, Error = error, IsConcurrencyError = isConcurrencyError };
}

public class Result<T> : Result
{
    public T? Data { get; set; }
}