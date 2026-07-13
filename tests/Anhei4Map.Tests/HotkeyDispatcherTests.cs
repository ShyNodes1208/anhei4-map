using Anhei4Map.Core.Models;
using Anhei4Map.Core.Services;

namespace Anhei4Map.Tests;

public class HotkeyDispatcherTests
{
    [Theory]
    [InlineData(1, HotkeyCommand.ToggleLock)]
    [InlineData(2, HotkeyCommand.ToggleHide)]
    [InlineData(3, HotkeyCommand.ZoomIn)]
    [InlineData(4, HotkeyCommand.ZoomOut)]
    [InlineData(5, HotkeyCommand.OpacityUp)]
    [InlineData(6, HotkeyCommand.OpacityDown)]
    [InlineData(7, HotkeyCommand.ResetPosition)]
    [InlineData(8, HotkeyCommand.Refresh)]
    public void Dispatch_KnownId_ReturnsCorrectCommand(int id, HotkeyCommand expected)
    {
        var bindings = AppSettings.CreateDefaults().Hotkeys;
        var dispatcher = new HotkeyDispatcher(bindings);

        Assert.Equal(expected, dispatcher.Dispatch(id));
    }

    [Fact]
    public void Dispatch_UnknownId_ReturnsUnknown()
    {
        var bindings = AppSettings.CreateDefaults().Hotkeys;
        var dispatcher = new HotkeyDispatcher(bindings);

        Assert.Equal(HotkeyCommand.Unknown, dispatcher.Dispatch(99));
    }

    [Fact]
    public void EmptyBindings_ReturnsUnknown()
    {
        var dispatcher = new HotkeyDispatcher([]);

        Assert.Equal(HotkeyCommand.Unknown, dispatcher.Dispatch(1));
        Assert.Equal(HotkeyCommand.Unknown, dispatcher.Dispatch(42));
    }

    [Fact]
    public void NullBindings_ReturnsUnknown()
    {
        var dispatcher = new HotkeyDispatcher(null);

        Assert.Equal(HotkeyCommand.Unknown, dispatcher.Dispatch(1));
        Assert.Equal(HotkeyCommand.Unknown, dispatcher.Dispatch(42));
    }

    [Fact]
    public void DuplicateIds_LastWins()
    {
        var bindings = new[]
        {
            new HotkeyBinding(1, 0, 0, HotkeyCommand.ToggleHide),
            new HotkeyBinding(1, 0, 0, HotkeyCommand.ToggleLock)
        };
        var dispatcher = new HotkeyDispatcher(bindings);

        Assert.Equal(HotkeyCommand.ToggleLock, dispatcher.Dispatch(1));
    }

    [Fact]
    public void Dispatch_IsDeterministic()
    {
        var bindings = AppSettings.CreateDefaults().Hotkeys;
        var dispatcher = new HotkeyDispatcher(bindings);

        var first = dispatcher.Dispatch(3);
        var second = dispatcher.Dispatch(3);

        Assert.Equal(first, second);
        Assert.Equal(HotkeyCommand.ZoomIn, first);
    }

    [Fact]
    public void Dispatch_AllEightDefaultCommands()
    {
        var bindings = AppSettings.CreateDefaults().Hotkeys;
        var dispatcher = new HotkeyDispatcher(bindings);

        var results = Enumerable.Range(1, 8)
            .Select(id => dispatcher.Dispatch(id))
            .ToArray();

        Assert.Equal(8, results.Distinct().Count());
        Assert.Contains(HotkeyCommand.ToggleLock, results);
        Assert.Contains(HotkeyCommand.ToggleHide, results);
        Assert.Contains(HotkeyCommand.ZoomIn, results);
        Assert.Contains(HotkeyCommand.ZoomOut, results);
        Assert.Contains(HotkeyCommand.OpacityUp, results);
        Assert.Contains(HotkeyCommand.OpacityDown, results);
        Assert.Contains(HotkeyCommand.ResetPosition, results);
        Assert.Contains(HotkeyCommand.Refresh, results);
    }
}
