namespace SapGui.Wrapper.Tests.Helpers;

/// <summary>
/// Fake raw COM object for a SAP GuiButton.
/// Used by <see cref="FakeComObject.WithButton"/> to simulate button children
/// inside a fake popup window during unit tests.
/// <para>
/// <see cref="WasPressed"/> is set to <see langword="true"/> when the wrapper
/// calls <c>Press()</c> via late binding, letting tests assert which button was clicked.
/// </para>
/// </summary>
internal sealed class FakeButtonObj
{
    /// <summary>SAP type string — must be "GuiButton" for <see cref="GuiMessageWindow.GetButtons"/> to pick it up.</summary>
    public string Type { get; } = "GuiButton";

    /// <summary>Component path used by <see cref="GuiComponent.Id"/>.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Button label read by <see cref="GuiComponent.Text"/>.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary><see langword="true"/> after <see cref="Press"/> has been called.</summary>
    public bool WasPressed { get; private set; }

    /// <summary>
    /// Simulates a button click. Called via late binding by <see cref="GuiComponent.Press"/>.
    /// </summary>
    public void Press() => WasPressed = true;
}

/// <summary>
/// Minimal fake children collection returned by <see cref="FakeComObject.Children"/>.
/// Exposes <c>Count</c> (property) and <c>Item(int)</c> (method) so that
/// <see cref="GuiMessageWindow.GetButtons"/> can enumerate buttons via late binding.
/// </summary>
internal sealed class FakeChildrenCollection
{
    private readonly IReadOnlyList<FakeButtonObj> _buttons;

    internal FakeChildrenCollection(IReadOnlyList<FakeButtonObj> buttons) =>
        _buttons = buttons;

    /// <summary>Number of children (read via <c>BindingFlags.GetProperty</c>).</summary>
    public int Count => _buttons.Count;

    /// <summary>Returns the child at <paramref name="index"/> (invoked via late binding).</summary>
    public FakeButtonObj Item(int index) => _buttons[index];
}
