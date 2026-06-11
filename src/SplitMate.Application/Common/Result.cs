namespace SplitMate.Application.Common;

/// <summary>Outcome of a command/query: success flag plus user-facing error messages.</summary>
public class Result
{
    public bool Succeeded { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];

    public static Result Success() => new() { Succeeded = true };
    public static Result Failure(params string[] errors) => new() { Errors = errors };
    public static Result Failure(IEnumerable<string> errors) => new() { Errors = errors.ToArray() };

    /// <summary>Builds a failed result of any concrete Result type (used by the validation pipeline).</summary>
    public static TResult Failure<TResult>(IEnumerable<string> errors) where TResult : Result, new()
        => new TResult { Succeeded = false, Errors = errors.ToArray() };
}

public class Result<T> : Result
{
    public T? Value { get; init; }

    public static Result<T> Success(T value) => new() { Succeeded = true, Value = value };
    public new static Result<T> Failure(params string[] errors) => new() { Errors = errors };
    public new static Result<T> Failure(IEnumerable<string> errors) => new() { Errors = errors.ToArray() };
}
