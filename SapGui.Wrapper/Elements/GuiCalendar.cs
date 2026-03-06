namespace SapGui.Wrapper;

/// <summary>
/// Wraps a SAP GUI calendar control (<c>GuiCalendar</c>).
/// Calendar controls appear in date selection dialogs and some screen elements.
/// </summary>
public class GuiCalendar : GuiComponent
{
    internal GuiCalendar(object raw) : base(raw) { }

    // SAP stores dates as "YYYYMMDD" strings on the FocusedDate property.
    private static string ToSapDate(DateTime d) => d.ToString("yyyyMMdd");

    private static DateTime? ParseSapDate(string s)
    {
        if (string.IsNullOrEmpty(s) || s.Length < 8) return null;
        return DateTime.TryParseExact(s, "yyyyMMdd",
                                      System.Globalization.CultureInfo.InvariantCulture,
                                      System.Globalization.DateTimeStyles.None,
                                      out var dt)
               ? dt
               : null;
    }

    /// <summary>
    /// The date that currently has keyboard focus in the calendar.
    /// Returns <see langword="null"/> if the value cannot be parsed.
    /// </summary>
    public DateTime? FocusedDate => ParseSapDate(GetString("FocusedDate"));

    /// <summary>
    /// Sets the focused/selected date in the calendar.
    /// Equivalent to clicking the given day in the SAP GUI calendar.
    /// </summary>
    public void SetDate(DateTime date) => SetProperty("FocusedDate", ToSapDate(date));

    /// <summary>
    /// Returns the currently focused/selected date, or
    /// <see langword="null"/> if nothing is selected or the date cannot be parsed.
    /// </summary>
    public DateTime? GetSelectedDate() => FocusedDate;

    /// <inheritdoc/>
    public override string ToString() =>
        $"Calendar [{Id}] FocusedDate={FocusedDate?.ToString("yyyy-MM-dd") ?? "(none)"}";
}
