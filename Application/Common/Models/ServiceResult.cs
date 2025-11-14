namespace Application.Common.Models;

public class ServiceResult
{
    public bool Success { get; protected set; }
    public string? Error { get; protected set; }

    public static ServiceResult Ok() => new() { Success = true };
    public static ServiceResult Fail(string error) => new() { Success = false, Error = error };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; private set; }

    public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };
    public new static ServiceResult<T> Fail(string error) => new() { Success = false, Error = error };
}