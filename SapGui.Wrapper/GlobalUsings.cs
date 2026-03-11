global using System.Reflection;
global using System.Runtime.InteropServices;
global using System.Diagnostics;
global using Microsoft.Extensions.Logging;

// Allow the test project to access internals (WrapComponent, GuiSessionInfo, etc.)
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("SapGui.Wrapper.Tests")]
