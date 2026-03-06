namespace SapGui.Wrapper;

/// <summary>
/// Wraps a SAP GUI tree control (GuiTree).
/// Tree controls appear in, e.g., the Easy Access menu or transport organizer.
/// </summary>
public class GuiTree : GuiComponent
{
    internal GuiTree(object raw) : base(raw) { }

    // ── Node navigation ───────────────────────────────────────────────────────

    /// <summary>
    /// Expands a tree node by its key.
    /// </summary>
    public void ExpandNode(string nodeKey)    => Invoke("ExpandNode", nodeKey);

    /// <summary>Collapses a tree node.</summary>
    public void CollapseNode(string nodeKey)  => Invoke("CollapseNode", nodeKey);

    /// <summary>Selects a tree node.</summary>
    public void SelectNode(string nodeKey)    => Invoke("SelectNode", nodeKey);

    /// <summary>Double-clicks a node (often used to open a transaction).</summary>
    public void DoubleClickNode(string nodeKey) => Invoke("DoubleClickNode", nodeKey);

    /// <summary>
    /// Returns the text of a node (its display label).
    /// </summary>
    public string GetNodeText(string nodeKey)
    {
        try
        {
            return (string?)Invoke("GetNodeTextByKey", nodeKey) ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    /// <summary>
    /// Returns the first-level child node keys of the given parent key.
    /// Pass an empty string or the root key to get top-level nodes.
    /// </summary>
    public IReadOnlyList<string> GetChildNodeKeys(string parentNodeKey)
    {
        try
        {
            var children = Invoke("GetSubNodesCol", parentNodeKey);
            if (children is null) return Array.Empty<string>();

            var t     = children.GetType();
            int count = (int)(t.InvokeMember("Count",
                                              BindingFlags.GetProperty,
                                              null, children, null) ?? 0);
            var result = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                var key = (string?)t.InvokeMember("Item",
                                                   BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                                                   null, children,
                                                   new object[] { i }) ?? string.Empty;
                result.Add(key);
            }
            return result;
        }
        catch { return Array.Empty<string>(); }
    }

    /// <summary>Gets the currently selected node key.</summary>
    public string SelectedNode
    {
        get
        {
            try
            {
                var sel = Invoke("GetSelectedNodes");
                if (sel is null) return string.Empty;
                var t = sel.GetType();
                return (string?)t.InvokeMember("Item",
                                               BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                                               null, sel,
                                               new object[] { 0 }) ?? string.Empty;
            }
            catch { return string.Empty; }
        }
    }

    /// <summary>
    /// Returns the text of a specific cell in a multi-column tree.
    /// </summary>
    /// <param name="nodeKey">The node key.</param>
    /// <param name="columnName">The technical column name.</param>
    public string GetItemText(string nodeKey, string columnName)
    {
        try { return (string?)Invoke("GetItemText", nodeKey, columnName) ?? string.Empty; }
        catch { return string.Empty; }
    }

    /// <summary>
    /// Returns every node key in the entire tree as a flat list.
    /// </summary>
    public IReadOnlyList<string> GetAllNodeKeys()
    {
        try
        {
            var col = Invoke("GetAllNodeKeys");
            if (col is null) return Array.Empty<string>();
            var t     = col.GetType();
            int count = (int)(t.InvokeMember("Count",
                                              BindingFlags.GetProperty,
                                              null, col, null) ?? 0);
            var result = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                var key = (string?)t.InvokeMember("Item",
                                                   BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                                                   null, col,
                                                   new object[] { i }) ?? string.Empty;
                result.Add(key);
            }
            return result;
        }
        catch { return Array.Empty<string>(); }
    }

    /// <summary>
    /// Opens the context menu for a node (equivalent to right-clicking it).
    /// </summary>
    public void NodeContextMenu(string nodeKey) => Invoke("NodeContextMenu", nodeKey);

    /// <summary>
    /// Returns the node type string for a given node key.
    /// Typical values are <c>"LEAF"</c> and <c>"FOLDER"</c>.
    /// </summary>
    public string GetNodeType(string nodeKey)
    {
        try { return (string?)Invoke("GetNodeType", nodeKey) ?? string.Empty; }
        catch { return string.Empty; }
    }
}
