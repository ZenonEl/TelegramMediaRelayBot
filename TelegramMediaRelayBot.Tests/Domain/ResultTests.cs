using TelegramMediaRelayBot.Domain.Common;

namespace TelegramMediaRelayBot.Tests.Domain;

public class ResultTests
{
    [Fact]
    public void Success_ShouldSetValueAndIsSuccess()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_ShouldSetErrorAndIsFailure()
    {
        var error = Error.Validation("test", "fail");
        var result = Result<int>.Failure(error);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Map_OnSuccess_ShouldTransformValue()
    {
        var result = Result<int>.Success(10);

        var mapped = result.Map(x => x * 2);

        Assert.True(mapped.IsSuccess);
        Assert.Equal(20, mapped.Value);
    }

    [Fact]
    public void Map_OnFailure_ShouldPropagateError()
    {
        var error = Error.NotFound("nf", "not found");
        var result = Result<int>.Failure(error);

        var mapped = result.Map(x => x.ToString());

        Assert.True(mapped.IsFailure);
        Assert.Equal(error, mapped.Error);
    }

    [Fact]
    public void Bind_OnSuccess_ShouldChainResults()
    {
        var result = Result<int>.Success(5);

        var bound = result.Bind(x =>
            x > 0 ? Result<string>.Success(x.ToString()) : Result<string>.Failure(Error.Validation("neg", "negative")));

        Assert.True(bound.IsSuccess);
        Assert.Equal("5", bound.Value);
    }

    [Fact]
    public void Bind_OnFailure_ShouldPropagateError()
    {
        var error = Error.Infrastructure("err", "infra error");
        var result = Result<int>.Failure(error);

        var bound = result.Bind(x => Result<string>.Success(x.ToString()));

        Assert.True(bound.IsFailure);
        Assert.Equal(error, bound.Error);
    }

    [Fact]
    public void Match_OnSuccess_ShouldCallOnSuccess()
    {
        var result = Result<int>.Success(7);

        var output = result.Match(
            onSuccess: v => $"ok:{v}",
            onFailure: e => $"err:{e.Code}");

        Assert.Equal("ok:7", output);
    }

    [Fact]
    public void Match_OnFailure_ShouldCallOnFailure()
    {
        var error = Error.External("ext", "external");
        var result = Result<string>.Failure(error);

        var output = result.Match(
            onSuccess: v => $"ok:{v}",
            onFailure: e => $"err:{e.Code}");

        Assert.Equal("err:ext", output);
    }

    [Fact]
    public void StaticSuccess_ShouldCreateSuccessResult()
    {
        var result = Result.Success("hello");

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void StaticFailure_ShouldCreateFailureResult()
    {
        var error = Error.Conflict("dup", "duplicate");
        var result = Result.Failure<int>(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }
}
