using NSubstitute;

namespace TelegramMediaRelayBot.Tests.Sessions;

public class UserSessionManagerTests : IDisposable
{
    private const long TestChatId = 999_000_001;

    public UserSessionManagerTests()
    {
        // Clean up before each test to avoid shared state leaking
        UserSessionManager.Remove(TestChatId);
        UserSessionManager.Remove(TestChatId + 1);
        UserSessionManager.Remove(TestChatId + 2);
    }

    public void Dispose()
    {
        UserSessionManager.Remove(TestChatId);
        UserSessionManager.Remove(TestChatId + 1);
        UserSessionManager.Remove(TestChatId + 2);
    }

    [Fact]
    public void Set_And_Get_ShouldStoreAndRetrieveState()
    {
        var state = Substitute.For<IUserState>();
        state.GetCurrentState().Returns("TestState");

        UserSessionManager.Set(TestChatId, state);
        var retrieved = UserSessionManager.Get(TestChatId);

        Assert.NotNull(retrieved);
        Assert.Equal("TestState", retrieved!.GetCurrentState());
    }

    [Fact]
    public void Get_NonExistentKey_ShouldReturnNull()
    {
        var result = UserSessionManager.Get(TestChatId + 2);

        Assert.Null(result);
    }

    [Fact]
    public void ContainsKey_ShouldReturnTrueWhenExists()
    {
        var state = Substitute.For<IUserState>();
        UserSessionManager.Set(TestChatId, state);

        Assert.True(UserSessionManager.ContainsKey(TestChatId));
    }

    [Fact]
    public void ContainsKey_ShouldReturnFalseWhenAbsent()
    {
        Assert.False(UserSessionManager.ContainsKey(TestChatId + 1));
    }

    [Fact]
    public void Remove_ShouldDeleteSession()
    {
        var state = Substitute.For<IUserState>();
        UserSessionManager.Set(TestChatId, state);

        var removed = UserSessionManager.Remove(TestChatId);

        Assert.True(removed);
        Assert.False(UserSessionManager.ContainsKey(TestChatId));
        Assert.Null(UserSessionManager.Get(TestChatId));
    }

    [Fact]
    public void Remove_WithOutParam_ShouldReturnRemovedState()
    {
        var state = Substitute.For<IUserState>();
        state.GetCurrentState().Returns("Removable");
        UserSessionManager.Set(TestChatId, state);

        var removed = UserSessionManager.Remove(TestChatId, out var removedState);

        Assert.True(removed);
        Assert.NotNull(removedState);
        Assert.Equal("Removable", removedState!.GetCurrentState());
    }
}
