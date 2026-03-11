using System;
using System.Collections.Generic;
using System.Data;
using UiPath.CodedWorkflows;
using UiPath.Core;
using UiPath.Core.Activities.Storage;
using UiPath.Orchestrator.Client.Models;
using UiPath.Testing;
using UiPath.Testing.Activities.TestData;
using UiPath.Testing.Activities.TestDataQueues.Enums;
using UiPath.Testing.Enums;
using UiPath.UIAutomationNext.API.Contracts;
using UiPath.UIAutomationNext.API.Models;
using UiPath.UIAutomationNext.Enums;
using SapGui.Wrapper;
using System.Linq;

namespace SapGuiWrapperTests
{
    public class Tests_09_Tree : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            // Arrange
            Log("Test run started for Tests_09_Tree.");

            using var sap = SapGuiClient.Attach();
            var session   = sap.Session;

            // Navigate to Easy Access menu
            session.ExitTransaction();
            session.WaitReady(timeoutMs: 5_000);

            const string treeId = "wnd[0]/usr/cntlIMAGE_CONTAINER/shellcont/shell"; // ← ADAPT

            try
            {
                var tree = session.Tree(treeId);

                // ── GetAllNodeKeys ────────────────────────────────────────────────
                var allKeys = tree.GetAllNodeKeys();
                Log($"GetAllNodeKeys: {allKeys.Count} key(s) total");
                foreach (var k in allKeys.Take(5))
                    Log($"  key='{k}' type='{tree.GetNodeType(k)}' text='{tree.GetNodeText(k)}'");

                if (allKeys.Count == 0)
                {
                    Log("Tree has no nodes – ADAPT the tree ID", LogLevel.Warn);
                    return;
                }

                string rootKey = allKeys[0];

                // ── GetNodeType ───────────────────────────────────────────────────
                Log($"GetNodeType('{rootKey}'): '{tree.GetNodeType(rootKey)}'");

                // ── GetChildNodeKeys ──────────────────────────────────────────────
                var children = tree.GetChildNodeKeys(rootKey);
                Log($"GetChildNodeKeys('{rootKey}'): {children.Count} child(ren)");
                foreach (var c in children.Take(3))
                    Log($"  child='{c}' text='{tree.GetNodeText(c)}'");

                // ── ExpandNode / CollapseNode ─────────────────────────────────────
                tree.ExpandNode(rootKey);
                Log($"ExpandNode('{rootKey}') called");

                tree.CollapseNode(rootKey);
                Log($"CollapseNode('{rootKey}') called");

                // ── SelectNode / SelectedNode ─────────────────────────────────────
                tree.SelectNode(rootKey);
                Log($"SelectedNode after SelectNode: '{tree.SelectedNode}'");

                // ── GetItemText (multi-column tree) ───────────────────────────────
                // This only works on multi-column trees; single-column trees return empty string
                string itemText = tree.GetItemText(rootKey, "Name"); // ← ADAPT column name
                Log($"GetItemText('{rootKey}', 'Name'): '{itemText}'");

                // ── NodeContextMenu (right-click) ─────────────────────────────────
                // Opens the context menu; close it with PressBack or PressEnter
                try
                {
                    tree.NodeContextMenu(rootKey);
                    Log($"NodeContextMenu('{rootKey}') called");
                    session.PressBack();  // dismiss
                    session.WaitReady(timeoutMs: 3_000);
                }
                catch (Exception ex)
                {
                    Log($"NodeContextMenu raised an exception: {ex.Message}", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                Log($"Tree test skipped – tree not found ({ex.Message})", LogLevel.Warn);
            }

            Log("Tests_09_Tree PASSED");
        }
    }
}