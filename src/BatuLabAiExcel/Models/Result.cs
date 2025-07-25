namespace BatuLabAiExcel.Models;

/// <summary>
/// Generic result type for operations that can succeed or fail
/// </summary>
/// <typeparam name="T">Type of the success value</typeparam>
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string? Error { get; private set; }
    public Exception? Exception { get; private set; }

    private Result(bool isSuccess, T? value, string? error, Exception? exception = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Exception = exception;
    }

    /// <summary>
    /// Create a successful result
    /// </summary>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>
    /// Create a failed result with error message
    /// </summary>
    public static Result<T> Failure(string error) => new(false, default, error);

    /// <summary>
    /// Create a failed result with exception
    /// </summary>
    public static Result<T> Failure(Exception exception) => 
        new(false, default, exception.Message, exception);

    /// <summary>
    /// Create a failed result with error message and exception
    /// </summary>
    public static Result<T> Failure(string error, Exception exception) => 
        new(false, default, error, exception);

    /// <summary>
    /// Transform the result value if successful
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
    {
        return IsSuccess && Value != null 
            ? Result<TOut>.Success(mapper(Value))
            : Result<TOut>.Failure(Error ?? "Value is null", Exception!);
    }

    /// <summary>
    /// Chain another operation that returns a Result
    /// </summary>
    public async Task<Result<TOut>> BindAsync<TOut>(Func<T, Task<Result<TOut>>> binder)
    {
        return IsSuccess && Value != null 
            ? await binder(Value)
            : Result<TOut>.Failure(Error ?? "Value is null", Exception!);
    }

    public override string ToString()
    {
        return IsSuccess 
            ? $"Success: {Value}"
            : $"Failure: {Error}";
    }
}

/// <summary>
/// Result type for operations that don't return a value
/// </summary>
public class Result
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }
    public Exception? Exception { get; private set; }

    private Result(bool isSuccess, string? error, Exception? exception = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Exception = exception;
    }

    /// <summary>
    /// Create a successful result
    /// </summary>
    public static Result Success() => new(true, null);

    /// <summary>
    /// Create a failed result with error message
    /// </summary>
    public static Result Failure(string error) => new(false, error);

    /// <summary>
    /// Create a failed result with exception
    /// </summary>
    public static Result Failure(Exception exception) => new(false, exception.Message, exception);

    /// <summary>
    /// Create a failed result with error message and exception
    /// </summary>
    public static Result Failure(string error, Exception exception) => new(false, error, exception);

    public override string ToString()
    {
        return IsSuccess ? "Success" : $"Failure: {Error}";
    }
}