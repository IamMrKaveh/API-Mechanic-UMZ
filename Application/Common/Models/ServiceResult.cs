namespace Application.Common.Models;

public class ServiceResult
{
    public bool Success { get; protected set; }
    public string? Error { get; protected set; }
    public int StatusCode { get; protected set; } = 200;

    public static ServiceResult Ok() => new() { Success = true };

    public static ServiceResult Fail(string error, int statusCode = 400) => new() { Success = false, Error = error, StatusCode = statusCode };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; private set; }

    public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data, StatusCode = 200 };

    public new static ServiceResult<T> Fail(string error, int statusCode = 400) => new() { Success = false, Error = error, StatusCode = statusCode };
}