using System.Runtime.InteropServices;

namespace SapGui.Wrapper.Com;

// Standard OLE connection-point COM interfaces.
// These are part of the Windows SDK (objidl.h) and are not included in
// .NET Core / .NET 6+ System.Runtime.InteropServices.ComTypes, so we
// declare them manually here using their well-known GUIDs.

[ComImport]
[Guid("B196B284-BAB4-101A-B69C-00AA00341D07")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IConnectionPoint
{
    void GetConnectionInterface(out Guid pIID);
    void GetConnectionPointContainer(out IConnectionPointContainer ppCPC);
    void Advise([MarshalAs(UnmanagedType.IUnknown)] object pUnkSink, out int pdwCookie);
    void Unadvise(int dwCookie);
    void EnumConnections(out object ppEnum);
}

[ComImport]
[Guid("B196B286-BAB4-101A-B69C-00AA00341D07")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IConnectionPointContainer
{
    void EnumConnectionPoints(out IEnumConnectionPoints ppEnum);
    void FindConnectionPoint(ref Guid riid, out IConnectionPoint ppCP);
}

[ComImport]
[Guid("B196B285-BAB4-101A-B69C-00AA00341D07")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IEnumConnectionPoints
{
    /// <summary>Fetches up to <paramref name="cConnections"/> entries.</summary>
    /// <returns>S_OK (0) when items were fetched; S_FALSE (1) when no more items.</returns>
    [PreserveSig]
    int Next(
        int cConnections,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IConnectionPoint[] rgpcn,
        out int pcFetched);

    void Skip(int cConnections);
    void Reset();
    void Clone(out IEnumConnectionPoints ppenum);
}
