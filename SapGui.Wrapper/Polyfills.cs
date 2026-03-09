// Polyfill: enables C# 9 'record' types to compile on .NET Framework 4.x and
// .NET Standard 2.x where System.Runtime.CompilerServices.IsExternalInit is absent.
// This class is internal and has no runtime cost — the compiler only needs it
// symbolically to emit the 'init' accessor modifier used by record primary constructors.
#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif
