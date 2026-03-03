namespace SapGui.Wrapper;

// ──────────────────────────────────────────────────────────────────────────────
// Custom exceptions
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>SAP GUI is not running, or scripting is not enabled.</summary>
public class SapGuiNotFoundException : Exception
{
    /// <summary>Initialises the exception with a message.</summary>
    /// <param name="message">Error description.</param>
    public SapGuiNotFoundException(string message) : base(message) { }

    /// <summary>Initialises the exception with a message and inner exception.</summary>
    /// <param name="message">Error description.</param>
    /// <param name="inner">Original exception.</param>
    public SapGuiNotFoundException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// A component with the given ID could not be found in the current session.
/// </summary>
public class SapComponentNotFoundException : Exception
{
    /// <summary>Initialises the exception with the component ID that was not found.</summary>
    /// <param name="componentId">The component ID path, e.g. <c>wnd[0]/usr/txtFoo</c>.</param>
    public SapComponentNotFoundException(string componentId)
        : base($"SAP GUI component not found: '{componentId}'") { }

    /// <summary>Initialises the exception with a component ID and inner exception.</summary>
    /// <param name="componentId">The component ID path.</param>
    /// <param name="inner">Original COM exception.</param>
    public SapComponentNotFoundException(string componentId, Exception inner)
        : base($"SAP GUI component not found: '{componentId}'", inner) { }
}
