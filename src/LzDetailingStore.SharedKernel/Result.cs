namespace LzDetailingStore.SharedKernel;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }
    public int? StatusCode { get; }

    protected Result(bool isSuccess, Error? error, int? statusCode = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        StatusCode = statusCode;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error, int statusCode = 400) => new(false, error, statusCode);
    public static Result NotFound(string message) => Failure(new Error("NOT_FOUND", message), 404);
    public static Result Conflict(string message) => Failure(new Error("CONFLICT", message), 409);
    public static Result Forbidden() => Failure(new Error("FORBIDDEN", "Access denied"), 403);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(true, null) => Value = value;
    private Result(Error error, int statusCode) : base(false, error, statusCode) { }

    public static Result<T> Success(T value) => new(value);
    public new static Result<T> Failure(Error error, int statusCode = 400) => new(error, statusCode);
    public new static Result<T> NotFound(string message) => new(new Error("NOT_FOUND", message), 404);
    public new static Result<T> Conflict(string message) => new(new Error("CONFLICT", message), 409);

    public static implicit operator Result<T>(T value) => Success(value);
}
