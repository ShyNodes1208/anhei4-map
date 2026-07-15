using Anhei4Map.Core.Models;

namespace Anhei4Map.Core.State;

public class WindowStateMachine
{
    private WindowState _currentState = WindowState.Locked;

    public WindowState CurrentState => _currentState;

    public bool CanEnterEdit => _currentState == WindowState.Locked;

    public bool ShouldApplyTransparent =>
        _currentState is WindowState.Hidden or WindowState.Locked;

    public bool ShouldRenderWebView =>
        _currentState is WindowState.Locked or WindowState.Edit;

    public bool ShouldShowEditOverlay =>
        _currentState == WindowState.Edit;

    public bool ShouldRunTopmostTimer =>
        _currentState == WindowState.Locked;

    public bool TryTransition(Trigger trigger)
    {
        switch (_currentState, trigger)
        {
            case (WindowState.Hidden, Trigger.ToggleHide):
                _currentState = WindowState.Locked;
                return true;
            case (WindowState.Locked, Trigger.ToggleHide):
                _currentState = WindowState.Hidden;
                return true;
            case (WindowState.Locked, Trigger.ToggleLock):
                _currentState = WindowState.Edit;
                return true;
            case (WindowState.Edit, Trigger.ToggleLock):
                _currentState = WindowState.Locked;
                return true;
            default:
                return false;
        }
    }
}
