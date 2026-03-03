global using System.Reflection;

// Allow the test project to access internals (WrapComponent, GuiSessionInfo, etc.)
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("SapGui.Wrapper.Tests")]
