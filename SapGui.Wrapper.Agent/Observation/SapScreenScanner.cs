namespace SapGui.Wrapper.Agent.Observation;

/// <summary>
/// Walks the entire SAP GUI COM component tree of an active session and
/// produces an immutable <see cref="SapScreenSnapshot"/>.
/// </summary>
internal sealed class SapScreenScanner
{
    private readonly GuiSession _session;

    /// <summary>
    /// Maximum recursion depth when walking the COM tree.
    /// SAP screens rarely exceed 8 levels; 12 is a safety ceiling.
    /// </summary>
    private const int MaxDepth = 12;

    public SapScreenScanner(GuiSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    // ── Public entry point ────────────────────────────────────────────────────

    /// <summary>
    /// Walks the entire COM component tree of the active window and all child
    /// windows (wnd[1], wnd[2], …), producing a complete <see cref="SapScreenSnapshot"/>.
    /// </summary>
    /// <param name="withScreenshot">
    /// When <see langword="true"/>, captures a HardCopy screenshot and
    /// embeds it as base64 PNG.  Adds ~300 ms — use only when passing the
    /// image to a vision-capable LLM.
    /// </param>
    public SapScreenSnapshot Scan(bool withScreenshot = false)
    {
        var builder = new SnapshotBuilder();

        // 1. Session identity from GuiSessionInfo
        var info = _session.Info;

        // 2. Walk wnd[0] recursively
        try
        {
            object wnd0Raw = _session.FindByIdDynamic("wnd[0]");
            WalkWindow(wnd0Raw, "wnd[0]", builder, isPopup: false);
        }
        catch { /* best-effort */ }

        // 3. Walk wnd[1]..wnd[9] as popups
        for (int w = 1; w <= 9; w++)
        {
            try
            {
                object wndRaw = _session.FindByIdDynamic($"wnd[{w}]");
                WalkWindow(wndRaw, $"wnd[{w}]", builder, isPopup: true);
            }
            catch { break; }  // no more windows
        }

        // 4. Capture statusbar
        var statusbar = new SapStatusbarSnapshot();
        try
        {
            var sb = _session.Statusbar();
            statusbar = new SapStatusbarSnapshot
            {
                Text        = sb.Text,
                MessageType = sb.MessageType,
            };
        }
        catch { /* ignore */ }

        // 5. Detect screen type
        var screenType = DetectScreenType(builder, info);

        // 6. Optional screenshot
        string? screenshotBase64 = null;
        if (withScreenshot)
            screenshotBase64 = CaptureScreenshot();

        // 7. Window title
        var title = string.Empty;
        try { title = _session.MainWindow().Title; }
        catch { /* ignore */ }

        return new SapScreenSnapshot
        {
            Transaction     = info.Transaction,
            WindowTitle     = title,
            SystemName      = info.SystemName,
            UserName        = info.User,
            Client          = info.Client,
            ScreenType      = screenType,
            CapturedAt      = DateTimeOffset.UtcNow,
            Statusbar       = statusbar,
            Fields          = builder.Fields,
            Buttons         = builder.Buttons,
            Grids           = builder.Grids,
            TabStrips       = builder.TabStrips,
            Trees           = builder.Trees,
            Menus           = builder.Menus,
            Popups          = builder.Popups,
            ScreenshotBase64 = screenshotBase64,
        };
    }

    // ── Window-level walker ───────────────────────────────────────────────────

    private void WalkWindow(object wndRaw, string path, SnapshotBuilder builder, bool isPopup)
    {
        if (isPopup)
        {
            var popupBuilder = new SnapshotBuilder();
            WalkContainer(wndRaw, path, popupBuilder, depth: 0);

            var popupTitle   = GetStringProp(wndRaw, "Text");
            var msgType      = string.Empty;
            var msgText      = string.Empty;

            // Try to read message type from the wnd itself (GuiMessageWindow)
            try
            {
                var wndType = GetStringProp(wndRaw, "Type");
                if (wndType == "GuiMessageWindow")
                {
                    msgType = GetStringProp(wndRaw, "MessageType");
                    msgText = GetStringProp(wndRaw, "MessageText");
                    if (string.IsNullOrEmpty(msgText))
                        msgText = popupBuilder.Fields
                            .Where(f => f.FieldType == "Label")
                            .Select(f => f.Value)
                            .FirstOrDefault() ?? string.Empty;
                }
            }
            catch { /* ignore */ }

            builder.Popups.Add(new SapPopupSnapshot
            {
                WindowId    = path,
                Title       = popupTitle,
                Message     = msgText,
                MessageType = msgType,
                Buttons     = popupBuilder.Buttons,
            });

            // Also promote the popup's interactive fields/grids to main builder
            // so they are visible to the agent (agent needs to interact with them)
            builder.Fields.AddRange(popupBuilder.Fields.Where(f => f.FieldType != "Label"));
            builder.Grids.AddRange(popupBuilder.Grids);
        }
        else
        {
            WalkContainer(wndRaw, path, builder, depth: 0);
        }
    }

    // ── Recursive container walker ────────────────────────────────────────────

    private void WalkContainer(object container, string path, SnapshotBuilder builder, int depth)
    {
        if (depth > MaxDepth) return;

        object? children;
        try
        {
            children = container.GetType()
                                .InvokeMember("Children",
                                              BindingFlags.GetProperty,
                                              null, container, null);
        }
        catch { return; }

        if (children is null) return;

        var ct    = children.GetType();
        int count;
        try
        {
            count = Convert.ToInt32(ct.InvokeMember("Count",
                BindingFlags.GetProperty, null, children, null) ?? 0);
        }
        catch { return; }

        for (int i = 0; i < count; i++)
        {
            object? child;
            try
            {
                child = ct.InvokeMember("Item",
                    BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                    null, children, new object[] { i });
            }
            catch { continue; }

            if (child is null) continue;

            var childType = GetStringProp(child, "Type");
            var childId   = GetStringProp(child, "Id");

            switch (childType)
            {
                // ── Text inputs / password fields ──────────────────────────────
                case "GuiTextField":
                case "GuiCTextField":
                case "GuiPasswordField":
                {
                    var label    = LabelResolver.Resolve(container, child, childId);
                    var value    = GetStringProp(child, "Text");
                    var maxLen   = GetIntProp(child, "MaxLength");
                    var readOnly = !GetBoolProp(child, "Changeable");
                    var required = GetBoolProp(child, "Required");
                    var visible  = GetBoolProp(child, "Visible");

                    builder.Fields.Add(new SapFieldSnapshot
                    {
                        Id         = childId,
                        Label      = label,
                        Value      = value,
                        FieldType  = childType == "GuiPasswordField" ? "PasswordField" : "TextField",
                        IsReadOnly = readOnly,
                        IsRequired = required,
                        IsVisible  = visible,
                        MaxLength  = maxLen,
                    });
                    break;
                }

                // ── Labels ────────────────────────────────────────────────────
                case "GuiLabel":
                {
                    var text    = GetStringProp(child, "Text");
                    var visible = GetBoolProp(child, "Visible");
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        builder.Fields.Add(new SapFieldSnapshot
                        {
                            Id        = childId,
                            Label     = text.TrimEnd(':').Trim(),
                            Value     = text,
                            FieldType = "Label",
                            IsReadOnly = true,
                            IsVisible  = visible,
                        });
                    }
                    break;
                }

                // ── Check box ─────────────────────────────────────────────────
                case "GuiCheckBox":
                {
                    var label    = LabelResolver.Resolve(container, child, childId);
                    if (string.IsNullOrEmpty(label))
                        label = GetStringProp(child, "Text");
                    var value    = GetBoolProp(child, "Selected") ? "true" : "false";
                    var readOnly = !GetBoolProp(child, "Changeable");
                    var visible  = GetBoolProp(child, "Visible");

                    builder.Fields.Add(new SapFieldSnapshot
                    {
                        Id         = childId,
                        Label      = label,
                        Value      = value,
                        FieldType  = "CheckBox",
                        IsReadOnly = readOnly,
                        IsVisible  = visible,
                    });
                    break;
                }

                // ── Radio button ──────────────────────────────────────────────
                case "GuiRadioButton":
                {
                    var label    = LabelResolver.Resolve(container, child, childId);
                    if (string.IsNullOrEmpty(label))
                        label = GetStringProp(child, "Text");
                    var value    = GetBoolProp(child, "Selected") ? "true" : "false";
                    var readOnly = !GetBoolProp(child, "Changeable");
                    var visible  = GetBoolProp(child, "Visible");

                    builder.Fields.Add(new SapFieldSnapshot
                    {
                        Id         = childId,
                        Label      = label,
                        Value      = value,
                        FieldType  = "RadioButton",
                        IsReadOnly = readOnly,
                        IsVisible  = visible,
                    });
                    break;
                }

                // ── Combo box ─────────────────────────────────────────────────
                case "GuiComboBox":
                {
                    var label    = LabelResolver.Resolve(container, child, childId);
                    var value    = GetStringProp(child, "Value");
                    if (string.IsNullOrEmpty(value))
                        value = GetStringProp(child, "Key");
                    var readOnly = !GetBoolProp(child, "Changeable");
                    var required = GetBoolProp(child, "Required");
                    var visible  = GetBoolProp(child, "Visible");
                    var options  = ReadComboOptions(child);

                    builder.Fields.Add(new SapFieldSnapshot
                    {
                        Id           = childId,
                        Label        = label,
                        Value        = value,
                        FieldType    = "ComboBox",
                        IsReadOnly   = readOnly,
                        IsRequired   = required,
                        IsVisible    = visible,
                        ComboOptions = options,
                    });
                    break;
                }

                // ── Standard push button ──────────────────────────────────────
                case "GuiButton":
                {
                    var text    = GetStringProp(child, "Text");
                    var tooltip = GetStringProp(child, "Tooltip");
                    var enabled = GetBoolProp(child, "Changeable");

                    builder.Buttons.Add(new SapButtonSnapshot
                    {
                        Id          = childId,
                        Text        = text,
                        Tooltip     = tooltip,
                        IsEnabled   = enabled,
                        ButtonType  = "GuiButton",
                    });
                    break;
                }

                // ── Toolbar ───────────────────────────────────────────────────
                case "GuiToolbar":
                    ReadToolbarButtons(child, childId, builder);
                    break;

                // ── ALV Grid ──────────────────────────────────────────────────
                case "GuiGridView":
                    builder.Grids.Add(ReadGrid(child, childId));
                    break;

                // ── Tree ──────────────────────────────────────────────────────
                case "GuiTree":
                    builder.Trees.Add(ReadTree(child, childId));
                    break;

                // ── Tab strip ─────────────────────────────────────────────────
                case "GuiTabStrip":
                    builder.TabStrips.Add(ReadTabStrip(child, childId, builder, depth));
                    break;

                // ── Menu bar ──────────────────────────────────────────────────
                case "GuiMenubar":
                    ReadMenubar(child, builder);
                    break;

                // ── HTML viewer / calendar (leaf; just flag) ─────────────────
                case "GuiHTMLViewer":
                    builder.HtmlViewer = true;
                    break;

                case "GuiCalendar":
                    builder.Calendar = true;
                    break;

                // ── Containers ────────────────────────────────────────────────
                case "GuiUserArea":
                case "GuiScrollContainer":
                case "GuiSplitterContainer":
                case "GuiContainerShell":
                case "GuiCustomControl":
                case "GuiShell":
                case "GuiTab":
                    WalkContainer(child, childId, builder, depth + 1);
                    break;

                // ── Classic table ─────────────────────────────────────────────
                case "GuiTable":
                case "GuiTableControl":
                    WalkContainer(child, childId, builder, depth + 1);
                    break;

                // ── Other containers (windows, frames) ────────────────────────
                case "GuiMainWindow":
                case "GuiFrameWindow":
                    WalkContainer(child, childId, builder, depth + 1);
                    break;

                default:
                    // Recurse into anything that might have children
                    WalkContainer(child, childId, builder, depth + 1);
                    break;
            }
        }
    }

    // ── Grid reader ───────────────────────────────────────────────────────────

    private static SapGridSnapshot ReadGrid(object raw, string id)
    {
        var grid   = new GuiGridView(raw);
        var cols   = new List<string>();
        try { cols.AddRange(grid.ColumnNames); }
        catch { /* ignore */ }

        var totalRows   = 0;
        var visibleRows = 0;
        var firstRow    = 0;
        try
        {
            totalRows   = grid.RowCount;
            visibleRows = grid.VisibleRowCount;
            firstRow    = grid.FirstVisibleRow;
        }
        catch { /* ignore */ }

        var rows = new List<SapGridRowSnapshot>();
        for (int r = firstRow; r < firstRow + visibleRows && r < totalRows; r++)
        {
            var cells = new Dictionary<string, string>(cols.Count);
            foreach (var col in cols)
            {
                try { cells[col] = grid.GetCellValue(r, col); }
                catch { cells[col] = string.Empty; }
            }
            rows.Add(new SapGridRowSnapshot { RowIndex = r, Cells = cells });
        }

        return new SapGridSnapshot
        {
            Id              = id,
            ColumnNames     = cols,
            VisibleRows     = rows,
            TotalRowCount   = totalRows,
            VisibleRowCount = visibleRows,
            FirstVisibleRow = firstRow,
        };
    }

    // ── Tree reader ───────────────────────────────────────────────────────────

    private static SapTreeSnapshot ReadTree(object raw, string id)
    {
        var nodes = new List<SapTreeNodeSnapshot>();
        try
        {
            var tree = new GuiTree(raw);
            ReadTreeLevel(tree, string.Empty, 0, nodes);
        }
        catch { /* ignore */ }

        return new SapTreeSnapshot { Id = id, VisibleNodes = nodes };
    }

    private static void ReadTreeLevel(GuiTree tree, string parentKey, int depth,
                                      List<SapTreeNodeSnapshot> nodes)
    {
        if (depth > 8) return; // safety limit
        try
        {
            var childKeys = tree.GetChildNodeKeys(parentKey);
            foreach (var key in childKeys)
            {
                var text        = tree.GetNodeText(key);
                var children    = tree.GetChildNodeKeys(key);
                var hasChildren = children.Count > 0;

                nodes.Add(new SapTreeNodeSnapshot
                {
                    NodeKey     = key,
                    Text        = text,
                    HasChildren = hasChildren,
                    Level       = depth,
                });

                if (hasChildren)
                    ReadTreeLevel(tree, key, depth + 1, nodes);
            }
        }
        catch { /* ignore */ }
    }

    // ── TabStrip reader ───────────────────────────────────────────────────────

    private SapTabStripSnapshot ReadTabStrip(object raw, string id,
                                             SnapshotBuilder builder, int depth)
    {
        var tabs = new List<SapTabSnapshot>();
        try
        {
            var ts  = new GuiTabStrip(raw);
            var all = ts.GetTabs();

            var selectedId = string.Empty;
            try
            {
                var selRaw = raw.GetType()
                                .InvokeMember("SelectedTab",
                                              BindingFlags.GetProperty,
                                              null, raw, null);
                if (selRaw is not null)
                    selectedId = GetStringProp(selRaw, "Id");
            }
            catch { /* ignore */ }

            foreach (var tab in all)
            {
                var tabId = tab.Id;
                var isSelected = selectedId == tabId || tab.Changeable == false;

                // Try a different way: check if tab is the active one
                try
                {
                    // SelectedTab property on raw
                    if (string.IsNullOrEmpty(selectedId))
                    {
                        var selTabText = GetStringProp(raw, "SelectedTab");
                        isSelected = selTabText == tab.Text;
                    }
                }
                catch { /* ignore */ }

                tabs.Add(new SapTabSnapshot
                {
                    Id         = tabId,
                    Text       = tab.Text,
                    IsSelected = isSelected,
                });

                // Walk child content of each tab
                WalkContainer(tab.RawObject, tabId, builder, depth + 1);
            }
        }
        catch { /* ignore */ }

        return new SapTabStripSnapshot { Id = id, Tabs = tabs };
    }

    // ── Toolbar reader ────────────────────────────────────────────────────────

    private static void ReadToolbarButtons(object raw, string toolbarId, SnapshotBuilder builder)
    {
        try
        {
            var children = raw.GetType()
                              .InvokeMember("Children",
                                            BindingFlags.GetProperty,
                                            null, raw, null);
            if (children is null) return;

            var ct    = children.GetType();
            int count = Convert.ToInt32(ct.InvokeMember("Count",
                BindingFlags.GetProperty, null, children, null) ?? 0);

            for (int i = 0; i < count; i++)
            {
                var btn = ct.InvokeMember("Item",
                    BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                    null, children, new object[] { i });
                if (btn is null) continue;

                var btnId   = GetStringProp(btn, "Id");
                var text    = GetStringProp(btn, "Text");
                var tooltip = GetStringProp(btn, "Tooltip");
                var fc      = GetStringProp(btn, "FunctionCode");
                var enabled = GetBoolProp(btn, "Enabled");

                builder.Buttons.Add(new SapButtonSnapshot
                {
                    Id           = btnId,
                    Text         = text,
                    Tooltip      = tooltip,
                    IsEnabled    = enabled,
                    ButtonType   = "ToolbarButton",
                    FunctionCode = fc,
                });
            }
        }
        catch { /* ignore */ }
    }

    // ── Menu bar reader ───────────────────────────────────────────────────────

    private static void ReadMenubar(object raw, SnapshotBuilder builder)
    {
        try
        {
            var children = raw.GetType()
                              .InvokeMember("Children",
                                            BindingFlags.GetProperty,
                                            null, raw, null);
            if (children is null) return;

            var ct    = children.GetType();
            int count = Convert.ToInt32(ct.InvokeMember("Count",
                BindingFlags.GetProperty, null, children, null) ?? 0);

            for (int i = 0; i < count; i++)
            {
                var item = ct.InvokeMember("Item",
                    BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                    null, children, new object[] { i });
                if (item is null) continue;

                builder.Menus.Add(ReadMenuNode(item, depth: 0));
            }
        }
        catch { /* ignore */ }
    }

    private static SapMenuSnapshot ReadMenuNode(object raw, int depth)
    {
        var menuId = GetStringProp(raw, "Id");
        var text   = GetStringProp(raw, "Text").TrimEnd('.');

        var children = new List<SapMenuSnapshot>();
        if (depth < 2)
        {
            try
            {
                var ch    = raw.GetType().InvokeMember("Children", BindingFlags.GetProperty, null, raw, null);
                if (ch is not null)
                {
                    var ct    = ch.GetType();
                    int count = Convert.ToInt32(ct.InvokeMember("Count", BindingFlags.GetProperty, null, ch, null) ?? 0);
                    for (int i = 0; i < count; i++)
                    {
                        var child = ct.InvokeMember("Item",
                            BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                            null, ch, new object[] { i });
                        if (child is not null)
                            children.Add(ReadMenuNode(child, depth + 1));
                    }
                }
            }
            catch { /* ignore */ }
        }

        return new SapMenuSnapshot { Id = menuId, Text = text, Children = children };
    }

    // ── Combo options reader ──────────────────────────────────────────────────

    private static IReadOnlyList<string> ReadComboOptions(object raw)
    {
        try
        {
            var entries = raw.GetType()
                             .InvokeMember("Entries",
                                           BindingFlags.GetProperty,
                                           null, raw, null);
            if (entries is null) return Array.Empty<string>();

            var et    = entries.GetType();
            int count = Convert.ToInt32(et.InvokeMember("Count",
                BindingFlags.GetProperty, null, entries, null) ?? 0);

            var list = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                var entry = et.InvokeMember("Item",
                    BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                    null, entries, new object[] { i });
                if (entry is null) continue;

                var key   = GetStringProp(entry, "Key");
                var value = GetStringProp(entry, "Value");
                list.Add(string.IsNullOrEmpty(value) ? key : value);
            }
            return list;
        }
        catch { return Array.Empty<string>(); }
    }

    // ── Screen type detector ──────────────────────────────────────────────────

    internal static SapScreenType DetectScreenType(SnapshotBuilder builder, GuiSessionInfo info)
    {
        var tcode = info.Transaction;

        // Login screen
        if (tcode == "S000" || tcode == "SMEN")
            return tcode == "S000" ? SapScreenType.Login : SapScreenType.EasyAccess;

        // Popup → MessageDialog or Dialog
        if (builder.Popups.Count > 0)
        {
            var first = builder.Popups[0];
            return string.IsNullOrEmpty(first.MessageType)
                ? SapScreenType.Dialog
                : SapScreenType.MessageDialog;
        }

        // Grid → AlvGrid
        if (builder.Grids.Count > 0)
            return SapScreenType.AlvGrid;

        // Tree → AlvTree or TreeNavigation
        if (builder.Trees.Count > 0)
            return SapScreenType.AlvTree;

        // HTML viewer
        if (builder.HtmlViewer)
            return SapScreenType.HtmlViewer;

        // Calendar
        if (builder.Calendar)
            return SapScreenType.Calendar;

        // Selection screen: mostly text fields + GuiLabel pairs + Execute button
        bool hasExecuteButton = builder.Buttons.Any(b =>
            b.FunctionCode == "EXEC" ||
            b.Tooltip.IndexOf("Execute", StringComparison.OrdinalIgnoreCase) >= 0 ||
            b.Text.IndexOf("Execute", StringComparison.OrdinalIgnoreCase) >= 0);

        if (hasExecuteButton && builder.Fields.Count > 2)
            return SapScreenType.SelectionScreen;

        // Screen title clues
        // (can't access window title here directly; rely on what's in the builder)
        // DefaultForm detection: has editable fields → EntryForm / DisplayForm
        var hasEditableFields = builder.Fields.Any(f =>
            !f.IsReadOnly && f.FieldType != "Label");

        if (hasEditableFields)
            return SapScreenType.EntryForm;

        if (builder.Fields.Any(f => f.FieldType != "Label"))
            return SapScreenType.DisplayForm;

        return SapScreenType.Unknown;
    }

    // ── Screenshot ────────────────────────────────────────────────────────────

    private string? CaptureScreenshot()
    {
        var tempFile = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"sap_screen_{Guid.NewGuid():N}.png");
        try
        {
            _session.MainWindow().HardCopy(tempFile, "PNG");
            if (!System.IO.File.Exists(tempFile)) return null;
            var bytes = System.IO.File.ReadAllBytes(tempFile);
            return Convert.ToBase64String(bytes);
        }
        catch { return null; }
        finally
        {
            try { if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile); }
            catch { /* ignore cleanup failure */ }
        }
    }

    // ── COM reflection helpers ────────────────────────────────────────────────

    private static string GetStringProp(object obj, string prop)
    {
        try
        {
            return (string?)obj.GetType()
                               .InvokeMember(prop,
                                             BindingFlags.GetProperty,
                                             null, obj, null) ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    private static bool GetBoolProp(object obj, string prop)
    {
        try
        {
            var val = obj.GetType()
                         .InvokeMember(prop, BindingFlags.GetProperty, null, obj, null);
            return val is not null && Convert.ToBoolean(val);
        }
        catch { return false; }
    }

    private static int GetIntProp(object obj, string prop)
    {
        try
        {
            var val = obj.GetType()
                         .InvokeMember(prop, BindingFlags.GetProperty, null, obj, null);
            return val is null ? 0 : Convert.ToInt32(val);
        }
        catch { return 0; }
    }

    // ── SnapshotBuilder helper ────────────────────────────────────────────────

    /// <summary>
    /// Mutable accumulator used while walking the COM tree.
    /// Converted to the immutable snapshot record after the walk is complete.
    /// </summary>
    internal sealed class SnapshotBuilder
    {
        public List<SapFieldSnapshot>    Fields    { get; } = new();
        public List<SapButtonSnapshot>   Buttons   { get; } = new();
        public List<SapGridSnapshot>     Grids     { get; } = new();
        public List<SapTabStripSnapshot> TabStrips { get; } = new();
        public List<SapTreeSnapshot>     Trees     { get; } = new();
        public List<SapMenuSnapshot>     Menus     { get; } = new();
        public List<SapPopupSnapshot>    Popups    { get; } = new();
        public bool                      HtmlViewer { get; set; }
        public bool                      Calendar   { get; set; }
    }
}
