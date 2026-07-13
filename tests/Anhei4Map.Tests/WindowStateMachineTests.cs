using Anhei4Map.Core.Models;
using Anhei4Map.Core.State;

namespace Anhei4Map.Tests;

public class WindowStateMachineTests
{
    [Fact]
    public void InitialState_IsLocked()
    {
        var sm = new WindowStateMachine();
        Assert.Equal(WindowState.Locked, sm.CurrentState);
    }

    [Fact]
    public void Locked_ToggleHide_TransitionsToHidden()
    {
        var sm = new WindowStateMachine();

        Assert.True(sm.TryTransition(Trigger.ToggleHide));
        Assert.Equal(WindowState.Hidden, sm.CurrentState);
    }

    [Fact]
    public void Hidden_ToggleHide_TransitionsToLocked()
    {
        var sm = new WindowStateMachine();
        sm.TryTransition(Trigger.ToggleHide);

        Assert.True(sm.TryTransition(Trigger.ToggleHide));
        Assert.Equal(WindowState.Locked, sm.CurrentState);
    }

    [Fact]
    public void Locked_ToggleLock_TransitionsToEdit()
    {
        var sm = new WindowStateMachine();

        Assert.True(sm.TryTransition(Trigger.ToggleLock));
        Assert.Equal(WindowState.Edit, sm.CurrentState);
    }

    [Fact]
    public void Edit_ToggleLock_TransitionsToLocked()
    {
        var sm = new WindowStateMachine();
        sm.TryTransition(Trigger.ToggleLock);

        Assert.True(sm.TryTransition(Trigger.ToggleLock));
        Assert.Equal(WindowState.Locked, sm.CurrentState);
    }

    [Fact]
    public void Hidden_ToggleLock_StaysHidden()
    {
        var sm = new WindowStateMachine();
        sm.TryTransition(Trigger.ToggleHide);

        Assert.False(sm.TryTransition(Trigger.ToggleLock));
        Assert.Equal(WindowState.Hidden, sm.CurrentState);
    }

    [Fact]
    public void Edit_ToggleHide_StaysEdit()
    {
        var sm = new WindowStateMachine();
        sm.TryTransition(Trigger.ToggleLock);

        Assert.False(sm.TryTransition(Trigger.ToggleHide));
        Assert.Equal(WindowState.Edit, sm.CurrentState);
    }

    [Theory]
    [InlineData(WindowState.Hidden, false)]
    [InlineData(WindowState.Locked, true)]
    [InlineData(WindowState.Edit, false)]
    public void Locked_CanEnterEdit(WindowState targetState, bool expected)
    {
        var sm = CreateInState(targetState);
        Assert.Equal(expected, sm.CanEnterEdit);
    }

    [Theory]
    [InlineData(WindowState.Hidden, true)]
    [InlineData(WindowState.Locked, true)]
    [InlineData(WindowState.Edit, false)]
    public void ShouldApplyTransparent_ByState(WindowState targetState, bool expected)
    {
        var sm = CreateInState(targetState);
        Assert.Equal(expected, sm.ShouldApplyTransparent);
    }

    [Theory]
    [InlineData(WindowState.Hidden, false)]
    [InlineData(WindowState.Locked, true)]
    [InlineData(WindowState.Edit, true)]
    public void ShouldRenderWebView_ByState(WindowState targetState, bool expected)
    {
        var sm = CreateInState(targetState);
        Assert.Equal(expected, sm.ShouldRenderWebView);
    }

    [Theory]
    [InlineData(WindowState.Hidden, false)]
    [InlineData(WindowState.Locked, false)]
    [InlineData(WindowState.Edit, true)]
    public void ShouldShowEditOverlay_ByState(WindowState targetState, bool expected)
    {
        var sm = CreateInState(targetState);
        Assert.Equal(expected, sm.ShouldShowEditOverlay);
    }

    [Theory]
    [InlineData(WindowState.Hidden, false)]
    [InlineData(WindowState.Locked, true)]
    [InlineData(WindowState.Edit, false)]
    public void ShouldRunTopmostTimer_ByState(WindowState targetState, bool expected)
    {
        var sm = CreateInState(targetState);
        Assert.Equal(expected, sm.ShouldRunTopmostTimer);
    }

    [Fact]
    public void MultipleTransitions_ProduceDeterministicResults()
    {
        var sm = new WindowStateMachine();
        Assert.Equal(WindowState.Locked, sm.CurrentState);

        Assert.True(sm.TryTransition(Trigger.ToggleLock));
        Assert.Equal(WindowState.Edit, sm.CurrentState);

        Assert.True(sm.TryTransition(Trigger.ToggleLock));
        Assert.Equal(WindowState.Locked, sm.CurrentState);

        Assert.True(sm.TryTransition(Trigger.ToggleHide));
        Assert.Equal(WindowState.Hidden, sm.CurrentState);

        Assert.True(sm.TryTransition(Trigger.ToggleHide));
        Assert.Equal(WindowState.Locked, sm.CurrentState);
    }

    [Fact]
    public void QueryProperties_DoNotChangeState()
    {
        var sm = new WindowStateMachine();

        _ = sm.CurrentState;
        _ = sm.CanEnterEdit;
        _ = sm.ShouldApplyTransparent;
        _ = sm.ShouldRenderWebView;
        _ = sm.ShouldShowEditOverlay;
        _ = sm.ShouldRunTopmostTimer;

        _ = sm.CurrentState;
        _ = sm.CanEnterEdit;
        _ = sm.ShouldApplyTransparent;
        _ = sm.ShouldRenderWebView;
        _ = sm.ShouldShowEditOverlay;
        _ = sm.ShouldRunTopmostTimer;

        Assert.Equal(WindowState.Locked, sm.CurrentState);
    }

    private static WindowStateMachine CreateInState(WindowState targetState)
    {
        var sm = new WindowStateMachine();
        switch (targetState)
        {
            case WindowState.Locked:
                return sm;
            case WindowState.Hidden:
                sm.TryTransition(Trigger.ToggleHide);
                return sm;
            case WindowState.Edit:
                sm.TryTransition(Trigger.ToggleLock);
                return sm;
            default:
                throw new ArgumentOutOfRangeException(nameof(targetState));
        }
    }
}
