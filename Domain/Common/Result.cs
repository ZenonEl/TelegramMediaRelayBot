namespace TelegramMediaRelayBot.Domain.Common;

public sealed class Result<T>
{
    private Result(T? value, Error? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }
    public Error? Error { get; }
    public bool IsSuccess => Error is null;
    public bool IsFailure => !IsSuccess;

    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Failure(Error error) => new(default, error);

    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
        => IsSuccess ? Result<TOut>.Success(mapper(Value!)) : Result<TOut>.Failure(Error!);

    public async Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> mapper)
        => IsSuccess ? Result<TOut>.Success(await mapper(Value!)) : Result<TOut>.Failure(Error!);

    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
        => IsSuccess ? binder(Value!) : Result<TOut>.Failure(Error!);

    public async Task<Result<TOut>> BindAsync<TOut>(Func<T, Task<Result<TOut>>> binder)
        => IsSuccess ? await binder(Value!) : Result<TOut>.Failure(Error!);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Error!);

    public async Task<TOut> MatchAsync<TOut>(Func<T, Task<TOut>> onSuccess, Func<Error, Task<TOut>> onFailure)
        => IsSuccess ? await onSuccess(Value!) : await onFailure(Error!);
}

public static class Result
{
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
}
