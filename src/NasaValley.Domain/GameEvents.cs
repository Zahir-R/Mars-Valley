namespace NasaValley.Domain;

/// <summary>
/// Observer hub. The simulation raises events; the presentation layer subscribes.
/// Nothing in the domain knows about Godot.
/// </summary>
public sealed class GameEvents
{
    /// <summary>Raised by <c>Say</c>; UI turns this into a toast + log entry.</summary>
    public event Action<string, MessageKind>? Message;

    public event Action? StateChanged;

    public event Action<bool>? GameEnded;

    /// <summary>UI request to focus a mission tab (e.g. opening the ship panel).</summary>
    public event Action<string>? PanelRequested;

    public void RaiseMessage(string text, MessageKind kind) => Message?.Invoke(text, kind);

    public void RaiseStateChanged() => StateChanged?.Invoke();

    public void RaiseGameEnded(bool win) => GameEnded?.Invoke(win);

    public void RaisePanelRequested(string tabId) => PanelRequested?.Invoke(tabId);
}
