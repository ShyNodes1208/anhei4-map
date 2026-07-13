using Anhei4Map.Core.Models;

namespace Anhei4Map.Core.Services;

public class HotkeyDispatcher
{
    private readonly IReadOnlyList<HotkeyBinding> _bindings;

    public HotkeyDispatcher(IEnumerable<HotkeyBinding>? bindings)
    {
        _bindings = bindings?.ToList() ?? [];
    }

    public HotkeyCommand Dispatch(int id)
    {
        HotkeyCommand? command = null;
        foreach (var binding in _bindings)
        {
            if (binding.Id == id)
            {
                command = binding.Command;
            }
        }

        return command ?? HotkeyCommand.Unknown;
    }
}
