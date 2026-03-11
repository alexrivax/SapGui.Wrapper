namespace SapGui.Wrapper;

/// <summary>
/// Represents a SAP GUI window (main window, modal dialog, etc.).
/// Equivalent to VBA <c>GuiFrameWindow</c> / <c>GuiMainWindow</c>.
/// </summary>
public class GuiMainWindow : GuiComponent
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr hWnd);

    internal GuiMainWindow(object raw) : base(raw) { }

    // ── Window properties ─────────────────────────────────────────────────────

    /// <summary>The window title bar text.</summary>
    public string Title      => GetString("Text");

    /// <summary>Returns <see langword="true"/> if this window is a modal dialog.</summary>
    public bool   IsDialog   => TypeName is "GuiModalWindow";

    /// <summary>Win32 window handle (HWND) of this SAP window.</summary>
    public IntPtr Handle     => new IntPtr(GetInt("Handle"));

    // ── Key actions ───────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a virtual key to the window.
    /// <list type="table">
    ///   <item><term>0</term><description>Enter</description></item>
    ///   <item><term>3</term><description>Back (F3)</description></item>
    ///   <item><term>4</term><description>Input Help (F4)</description></item>
    ///   <item><term>5</term><description>Matchcode (Search)</description></item>
    ///   <item><term>8</term><description>Execute (F8)</description></item>
    ///   <item><term>11</term><description>Save (Ctrl+S)</description></item>
    ///   <item><term>12</term><description>Exit (F12 / Shift+F4)</description></item>
    ///   <item><term>15</term><description>Cancel (F15 / Shift+F3)</description></item>
    ///   <item><term>71</term><description>Scroll to top</description></item>
    ///   <item><term>82</term><description>Scroll to bottom</description></item>
    /// </list>
    /// </summary>
    public void SendVKey(int vKey) => Invoke("SendVKey", vKey);

    /// <summary>Maximises the window.</summary>
    public void Maximize() => Invoke("Maximize");

    /// <summary>Minimizes (iconifies) the window to the taskbar.</summary>
    public void Iconify()  => Invoke("Iconify");

    /// <summary>Restores the window to normal size.</summary>
    public void Restore()  => Invoke("Restore");

    /// <summary>Closes the window (equivalent to the X button).</summary>
    public void Close()    => Invoke("Close");

    /// <summary>
    /// Returns <see langword="true"/> if the window is currently maximized.
    /// Determined via the Win32 <c>IsZoomed</c> API on the window handle,
    /// which is reliable across all SAP GUI versions (the SAP COM property
    /// <c>IsMaximized</c> is not consistently updated after <c>Maximize()</c>).
    /// </summary>
    public bool IsMaximized => IsZoomed(Handle);

    // ── Screenshot ────────────────────────────────────────────────────────────

    /// <summary>
    /// Takes a screenshot and saves it to a file.
    /// Format is determined by the file extension (.png, .bmp, .jpg).
    /// </summary>
    public void HardCopy(string filePath, string format = "PNG") =>
        Invoke("HardCopy", filePath, format);
}
