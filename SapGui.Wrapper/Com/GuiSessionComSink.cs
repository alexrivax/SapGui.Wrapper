using System.Globalization;
using System.Runtime.InteropServices;

namespace SapGui.Wrapper.Com;

/// <summary>
/// COM event sink for a SAP GUI session, implemented via <see cref="IReflect"/>.
/// <para>
/// When connected, SAP GUI calls <c>IDispatch::Invoke</c> on this object for
/// every session event (<c>Change</c>, <c>Destroy</c>, <c>AbapRuntimeError</c>,
/// <c>StartRequest</c>, <c>EndRequest</c>).  .NET routes those calls through
/// <see cref="IReflect.InvokeMember"/>, which this class implements to fire the
/// corresponding .NET events on the owning <see cref="GuiSession"/>.
/// </para>
/// <para>
/// The sink connects to <b>all</b> available connection points exposed by the
/// raw COM session object (typically just one) by enumerating
/// <c>IConnectionPointContainer::EnumConnectionPoints</c>. This avoids the
/// need to hard-code the SAP session events interface IID.
/// </para>
/// <para>
/// Call <see cref="IsConnected"/> after construction to determine whether the
/// sink successfully hooked the COM object.  If not connected, the caller
/// should fall back to the polling-based <c>SessionEventMonitor</c>.
/// </para>
/// </summary>
[ComVisible(true)]
internal sealed class GuiSessionComSink : IReflect, IDisposable
{
    private readonly GuiSession _session;
    private readonly List<IConnectionPoint> _connectionPoints = new();
    private readonly List<int>              _cookies          = new();
    private bool _disposed;

    // ── Construction / connection ─────────────────────────────────────────────

    internal GuiSessionComSink(GuiSession session, object rawSession)
    {
        _session = session;
        TryConnect(rawSession);
    }

    /// <summary>
    /// <see langword="true"/> when the sink has successfully advised at least
    /// one connection point on the COM session object.
    /// </summary>
    internal bool IsConnected => _connectionPoints.Count > 0;

    private void TryConnect(object rawSession)
    {
        if (rawSession is not IConnectionPointContainer cpc) return;

        try
        {
            cpc.EnumConnectionPoints(out var enumerator);
            var buffer = new IConnectionPoint[1];

            while (enumerator.Next(1, buffer, out int fetched) == 0 && fetched > 0)
            {
                try
                {
                    buffer[0].Advise(this, out int cookie);
                    _connectionPoints.Add(buffer[0]);
                    _cookies.Add(cookie);
                }
                catch { /* this connection point doesn't accept our sink – skip */ }
            }
        }
        catch { /* COM object doesn't implement IConnectionPointContainer */ }
    }

    // ── IReflect – COM dispatch routing ──────────────────────────────────────
    // SAP GUI calls IDispatch::Invoke on our CCW.  .NET routes each call here.
    // Events may arrive by name ("Change") or by DISPID ("[DISPID=1]").

    Type IReflect.UnderlyingSystemType => GetType();

    FieldInfo?    IReflect.GetField(string name, BindingFlags f) => null;
    FieldInfo[]   IReflect.GetFields(BindingFlags f) => Array.Empty<FieldInfo>();
    MemberInfo[]  IReflect.GetMember(string name, BindingFlags f) => Array.Empty<MemberInfo>();
    MemberInfo[]  IReflect.GetMembers(BindingFlags f) => Array.Empty<MemberInfo>();
    MethodInfo?   IReflect.GetMethod(string name, BindingFlags f) => null;
    MethodInfo?   IReflect.GetMethod(string name, BindingFlags f, Binder? b, Type[] types, ParameterModifier[]? pms) => null;
    MethodInfo[]  IReflect.GetMethods(BindingFlags f) => Array.Empty<MethodInfo>();
    PropertyInfo? IReflect.GetProperty(string name, BindingFlags f) => null;
    PropertyInfo? IReflect.GetProperty(string name, BindingFlags f, Binder? b, Type? ret, Type[]? types, ParameterModifier[]? pms) => null;
    PropertyInfo[] IReflect.GetProperties(BindingFlags f) => Array.Empty<PropertyInfo>();

    object? IReflect.InvokeMember(
        string name,
        BindingFlags flags,
        Binder? binder,
        object? target,
        object?[]? args,
        ParameterModifier[]? modifiers,
        CultureInfo? culture,
        string[]? namedParameters)
    {
        switch (ResolveEventName(name))
        {
            case SapEvent.Change:
                HandleChange(args);
                break;

            case SapEvent.Destroy:
                HandleDestroy();
                break;

            case SapEvent.AbapRuntimeError:
                HandleAbapRuntimeError(args);
                break;

            case SapEvent.StartRequest:
                HandleStartRequest(args);
                break;

            case SapEvent.EndRequest:
                HandleEndRequest(args);
                break;
        }
        return null;
    }

    // ── SAP event handlers ────────────────────────────────────────────────────

    private void HandleChange(object?[]? args)
    {
        // Change(GuiSession session, GuiChangeArgs args)
        object? changeArgs = args?.Length > 1 ? args[1] : null;
        string text         = GetStringProp(changeArgs, "Text");
        string functionCode = GetStringProp(changeArgs, "FunctionCode");
        string messageType  = GetStringProp(changeArgs, "MessageType");

        _session.RaiseChange(new SessionChangeEventArgs(text, functionCode, messageType));

        // AbapRuntimeError is also signalled via message type 'A'
        if (messageType == "A")
            _session.RaiseAbapRuntimeError(new AbapRuntimeErrorEventArgs(text));
    }

    private void HandleDestroy() => _session.RaiseDestroy();

    private void HandleAbapRuntimeError(object?[]? args)
    {
        // AbapRuntimeError(GuiSession session [, GuiAbapRuntimeErrorArgs args])
        object? errArgs = args?.Length > 1 ? args[1] : (args?.Length > 0 ? args[0] : null);
        string msg = GetStringProp(errArgs, "Message");
        if (string.IsNullOrEmpty(msg)) msg = GetStringProp(errArgs, "Text");
        _session.RaiseAbapRuntimeError(new AbapRuntimeErrorEventArgs(msg));
    }

    private void HandleStartRequest(object?[]? args)
    {
        // StartRequest(GuiSession session, GuiStartRequestArgs args)
        object? reqArgs = args?.Length > 1 ? args[1] : null;
        _session.RaiseStartRequest(new StartRequestEventArgs(GetStringProp(reqArgs, "Text")));
    }

    private void HandleEndRequest(object?[]? args)
    {
        // EndRequest(GuiSession session, GuiEndRequestArgs args)
        object? reqArgs = args?.Length > 1 ? args[1] : null;
        string text         = GetStringProp(reqArgs, "Text");
        string functionCode = GetStringProp(reqArgs, "FunctionCode");
        string messageType  = GetStringProp(reqArgs, "MessageType");
        _session.RaiseEndRequest(new EndRequestEventArgs(text, functionCode, messageType));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private enum SapEvent { Unknown, Change, Destroy, AbapRuntimeError, SystemError, StartRequest, EndRequest }

    /// <summary>
    /// Resolves either a method name or a "[DISPID=n]" string to a known SAP event.
    /// SAP may call Invoke either by name (after GetIDsOfNames) or by DISPID directly.
    /// </summary>
    private static SapEvent ResolveEventName(string name)
    {
        // Named call
        if (!name.StartsWith("[DISPID=", StringComparison.Ordinal))
            return name.ToUpperInvariant() switch
            {
                "CHANGE"            => SapEvent.Change,
                "DESTROY"           => SapEvent.Destroy,
                "ABAPRUNTIMEERROR"  => SapEvent.AbapRuntimeError,
                "SYSTEMERROR"       => SapEvent.SystemError,
                "STARTREQUEST"      => SapEvent.StartRequest,
                "ENDREQUEST"        => SapEvent.EndRequest,
                _                   => SapEvent.Unknown,
            };

        // DISPID-based call: "[DISPID=n]"
        if (name.EndsWith("]") && name.Length > 9 &&
            int.TryParse(name.Substring(8, name.Length - 9), out int dispid))
        {
            return dispid switch
            {
                1 => SapEvent.Change,
                2 => SapEvent.Destroy,
                3 => SapEvent.AbapRuntimeError,
                4 => SapEvent.SystemError,
                5 => SapEvent.StartRequest,
                6 => SapEvent.EndRequest,
                _ => SapEvent.Unknown,
            };
        }

        return SapEvent.Unknown;
    }

    private static string GetStringProp(object? comObj, string property)
    {
        if (comObj is null) return string.Empty;
        try
        {
            return (string?)(comObj.GetType()
                                   .InvokeMember(property,
                                                 BindingFlags.GetProperty,
                                                 null, comObj, null)) ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        for (int i = 0; i < _connectionPoints.Count; i++)
        {
            try { _connectionPoints[i].Unadvise(_cookies[i]); }
            catch { /* ignore COM errors during cleanup */ }
        }
        _connectionPoints.Clear();
        _cookies.Clear();
    }
}
