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

/// <summary>
/// Thrown when <c>FieldFinder</c> or <c>ButtonFinder</c> in the agent layer
/// cannot resolve a semantic target to a SAP GUI element.
/// </summary>
public sealed class SapAgentResolutionException : Exception
{
    /// <summary>The semantic target string that could not be resolved.</summary>
    public string Target { get; }

    /// <summary>The element type being resolved: <c>"field"</c>, <c>"button"</c>, etc.</summary>
    public string ElementType { get; }

    /// <summary>Candidate elements that were available at the time of the resolution attempt.</summary>
    public IReadOnlyList<string> Candidates { get; }

    /// <summary>Initialises the exception.</summary>
    public SapAgentResolutionException(
        string target,
        string elementType,
        IReadOnlyList<string>? candidates = null)
        : base(BuildMessage(target, elementType, candidates))
    {
        Target = target;
        ElementType = elementType;
        Candidates = candidates ?? Array.Empty<string>();
    }

    private static string BuildMessage(string target, string elementType, IReadOnlyList<string>? candidates)
    {
        var msg = $"Agent could not resolve {elementType} \"{target}\".";
        if (candidates is { Count: > 0 })
            msg += $" Available: {string.Join(", ", candidates.Take(10).Select(c => $"\"{c}\""))}.";
        return msg;
    }
}

/// <summary>
/// Thrown when the SAP agent coordinator is blocked by a safety rule
/// (e.g. <c>ReadOnlyMode</c>, blocked transaction code, or destructive action guard).
/// </summary>
public sealed class SapAgentBlockedException : Exception
{
    /// <summary>The safety rule that blocked execution.</summary>
    public string Reason { get; }

    /// <summary>Initialises the exception with a reason string.</summary>
    public SapAgentBlockedException(string reason)
        : base($"Agent action blocked: {reason}")
    {
        Reason = reason;
    }
}
