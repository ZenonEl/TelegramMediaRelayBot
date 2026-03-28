using TelegramMediaRelayBot.Domain.Common;

namespace TelegramMediaRelayBot.Tests.Domain;

public class ErrorTests
{
    [Fact]
    public void Validation_ShouldCreateValidationError()
    {
        var error = Error.Validation("val.001", "invalid input");

        Assert.Equal("val.001", error.Code);
        Assert.Equal("invalid input", error.Message);
        Assert.Equal(ErrorType.Validation, error.Type);
    }

    [Fact]
    public void NotFound_ShouldCreateNotFoundError()
    {
        var error = Error.NotFound("nf.001", "resource missing");

        Assert.Equal(ErrorType.NotFound, error.Type);
    }

    [Fact]
    public void Infrastructure_ShouldCreateInfrastructureError()
    {
        var error = Error.Infrastructure("infra.001", "db down");

        Assert.Equal(ErrorType.Infrastructure, error.Type);
        Assert.Equal("infra.001", error.Code);
        Assert.Equal("db down", error.Message);
    }

    [Fact]
    public void EqualErrors_ShouldBeEqual()
    {
        var a = Error.Validation("code", "msg");
        var b = Error.Validation("code", "msg");

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void DifferentErrors_ShouldNotBeEqual()
    {
        var a = Error.Validation("code", "msg");
        var b = Error.NotFound("code", "msg");

        Assert.NotEqual(a, b);
    }
}
