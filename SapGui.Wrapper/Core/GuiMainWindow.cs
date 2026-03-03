namespace SapGui.Wrapper;

/// <summary>
/// Represents a SAP GUI window (main window, modal dialog, etc.).
/// Equivalent to VBA <c>GuiFrameWindow</c> / <c>GuiMainWindow</c>.
/// </summary>
public class GuiMainWindow : GuiComponent
{
    internal GuiMainWindow(object raw) : base(raw) { }

    // ── Window properties ─────────────────────────────────────────────────────

    /// <summary>The window title bar text.</summary>
    public string Title      => GetString("Text");

    /// <summary>Returns <see langword="true"/> if this window is a modal dialog.</summary>
    public bool   IsDialog   => TypeName is "GuiModalWindow";

    // ── Key actions ───────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a virtual key to the window.
    /// <list type="table">
    ///   <item><term>0</term><description>Enter</description></item>
    ///   <item><term>3</term><description>Back (F3)</description></item>
    ///   <item><term>4</term><description>End (F4)</description></item>
    ///   <item><term>5</term><description>Matchcode (Search)</description></item>
    ///   <item><term>8</term><description>Save (Ctrl+S)</description></item>
    ///   <item><term>12</term><description>Exit (F12 / Shift+F4)</description></item>
    ///   <item><term>15</term><description>Cancel (F15 / Shift+F3)</description></item>
    ///   <item><term>71</term><description>Scroll to top</description></item>
    ///   <item><term>82</term><description>Scroll to bottom</description></item>
    /// </list>
    /// </summary>
    public void SendVKey(int vKey) => Invoke("SendVKey", vKey);

    /// <summary>Maximises the window.</summary>
    public void Maximize() => Invoke("Maximize");

    /// <summary>Restores the window to normal size.</summary>
    public void Restore()  => Invoke("Restore");

    /// <summary>Closes the window (equivalent to the X button).</summary>
    public void Close()    => Invoke("Close");

    // ── Screenshot ────────────────────────────────────────────────────────────

    /// <summary>
    /// Takes a screenshot and saves it to a file.
    /// Format is determined by the file extension (.png, .bmp, .jpg).
    /// </summary>
    public void HardCopy(string filePath, string format = "PNG") =>
        Invoke("HardCopy", filePath, format);
}
