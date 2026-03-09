using System.Runtime.InteropServices.ComTypes;

namespace SapGui.Wrapper.Com;

/// <summary>
/// Low-level access to the SAP Running Object Table (ROT).
/// SAP GUI registers itself under the "SAPGUI" moniker when running.
/// Two techniques are supported:
///   1. SapROTWr.CSapROTWrapper  – officially recommended by SAP.
///   2. GetActiveObject via ROT P/Invoke – fallback when CSapROTWrapper is absent.
/// </summary>
internal static class SapRot
{
    private const string SapGuiMoniker = "SAPGUI";
    private const string RotWrapperProg = "SapROTWr.CSapROTWrapper";

    // P/Invoke declarations needed for the fallback on .NET 6+
    // (Marshal.GetActiveObject was removed in .NET 5+)
    [DllImport("ole32.dll")]
    private static extern int GetRunningObjectTable(uint reserved, out IRunningObjectTable pprot);

    [DllImport("ole32.dll")]
    private static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

    /// <summary>
    /// Returns the raw COM <c>GuiApplication</c> object from whichever
    /// technique succeeds first.  Throws <see cref="SapGuiNotFoundException"/>
    /// when SAP GUI is not running or scripting is disabled.
    /// </summary>
    internal static object GetGuiApplication()
    {
        // Technique 1: official SAP ROT wrapper (SapROTWr.dll ships with SAPGUI)
        try
        {
            var rotWrapperType = Type.GetTypeFromProgID(RotWrapperProg, throwOnError: true)!;
            var rotWrapper = Activator.CreateInstance(rotWrapperType)!;
            try
            {
                var sapGuiRaw = rotWrapper.GetType()
                                           .InvokeMember("GetROTEntry",
                                                         BindingFlags.InvokeMethod,
                                                         null, rotWrapper,
                                                         new object[] { SapGuiMoniker })!;
                try
                {
                    var engine = sapGuiRaw.GetType()
                                          .InvokeMember("GetScriptingEngine",
                                                        BindingFlags.InvokeMethod,
                                                        null, sapGuiRaw, null)!;
                    return engine;
                }
                finally
                {
                    // Release the intermediate SAPGUI ROT entry – the engine holds its own ref.
                    if (Marshal.IsComObject(sapGuiRaw))
                        Marshal.ReleaseComObject(sapGuiRaw);
                }
            }
            finally
            {
                // Release the ROT wrapper helper object – no longer needed.
                if (Marshal.IsComObject(rotWrapper))
                    Marshal.ReleaseComObject(rotWrapper);
            }
        }
        catch (Exception ex) when (ex is not SapGuiNotFoundException)
        {
            // fall through to technique 2
        }

        // Technique 2: walk the ROT directly via P/Invoke
        // (equivalent to Marshal.GetActiveObject which was removed in .NET 5+)
        try
        {
            var sapGuiRaw = GetActiveObjectFromRot(SapGuiMoniker)
                            ?? throw new COMException($"'{SapGuiMoniker}' not found in ROT.");

            try
            {
                var engine = sapGuiRaw.GetType()
                                      .InvokeMember("GetScriptingEngine",
                                                    BindingFlags.InvokeMethod,
                                                    null, sapGuiRaw, null)!;
                return engine;
            }
            finally
            {
                // Release the intermediate ROT object – the engine holds its own ref.
                if (Marshal.IsComObject(sapGuiRaw))
                    Marshal.ReleaseComObject(sapGuiRaw);
            }
        }
        catch (Exception ex) when (ex is not SapGuiNotFoundException)
        {
            throw new SapGuiNotFoundException(
                "SAP GUI is not running or GUI scripting is not enabled. " +
                "Enable it via SAP GUI Options → Accessibility & Scripting → Scripting.", ex);
        }
    }

    /// <summary>
    /// Walks the Windows Running Object Table and returns the object registered
    /// under the given display name, or <c>null</c> if not found.
    /// This is a cross-framework replacement for <c>Marshal.GetActiveObject</c>.
    /// </summary>
    private static object? GetActiveObjectFromRot(string monikerName)
    {
        GetRunningObjectTable(0, out var rot);
        try
        {
            rot.EnumRunning(out var enumMoniker);
            try
            {
                var monikers = new IMoniker[1];
                var fetched = IntPtr.Zero;

                while (enumMoniker.Next(1, monikers, fetched) == 0)
                {
                    CreateBindCtx(0, out var ctx);
                    try
                    {
                        monikers[0].GetDisplayName(ctx, null, out var displayName);

                        if (string.Equals(displayName, monikerName, StringComparison.OrdinalIgnoreCase))
                        {
                            rot.GetObject(monikers[0], out var obj);
                            return obj;
                        }
                    }
                    finally
                    {
                        // Release the bind context created for each moniker name lookup.
                        if (Marshal.IsComObject(ctx))
                            Marshal.ReleaseComObject(ctx);
                    }
                }
            }
            finally
            {
                // Release the enumerator COM object after enumeration completes.
                if (Marshal.IsComObject(enumMoniker))
                    Marshal.ReleaseComObject(enumMoniker);
            }
        }
        finally
        {
            // Release the ROT itself after enumeration.
            if (Marshal.IsComObject(rot))
                Marshal.ReleaseComObject(rot);
        }

        return null;
    }
}
