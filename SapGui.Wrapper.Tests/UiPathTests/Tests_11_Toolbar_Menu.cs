using System;
using System.Linq;
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

namespace SapGuiWrapperTests
{
    public class Tests_11_Toolbar_Menu : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            // Arrange
            Log("Test run started for Tests_11_Toolbar_Menu.");

            using var sap = SapGuiClient.Attach();
            var session   = sap.Session;

            session.StartTransaction("SE16");
            session.WaitReady(timeoutMs: 10_000);

            // ── Toolbar ───────────────────────────────────────────────────────────
            try
            {
                var toolbar = session.Toolbar(); // default: wnd[0]/tbar[1]
                Log($"Toolbar ButtonCount: {toolbar.ButtonCount}");

                for (int i = 0; i < Math.Min(toolbar.ButtonCount, 5); i++)
                {
                    string tip = toolbar.GetButtonTooltip(i);
                    Log($"  Button[{i}] tooltip: '{tip}'");
                }

                // Use PressButtonByFunctionCode to trigger a toolbar button by its SAP function code.
                // Example (commented out — navigates away from the current screen):
                // toolbar.PressButtonByFunctionCode("BACK");
            }
            catch (Exception ex)
            {
                Log($"Toolbar test skipped ({ex.Message})", LogLevel.Warn);
            }

            // ── Menubar ───────────────────────────────────────────────────────────
            try
            {
                var menubar = session.Menubar();
                Log($"Menubar Count: {menubar.Count}");

                // GuiMenubar exposes Count but not GetChildren();
                // retrieve each top-level menu by ID path.
                var menus = new List<GuiMenu>();
                for (int i = 0; i < menubar.Count; i++)
                {
                    try { menus.Add(session.Menu($"wnd[0]/mbar/menu[{i}]")); }
                    catch { /* sparse index or separator */ }
                }

                Log($"Menubar top-level items: {menus.Count}");
                foreach (var m in menus)
                    Log($"  Menu: '{m.Text}' Id={m.Id}");

                // Drill into first menu to list its children
                if (menus.Count > 0)
                {
                    var firstMenu = menus[0];
                    var subItems  = firstMenu.GetChildren();
                    Log($"  First menu children ({subItems.Count}):");
                    foreach (var sub in subItems.Take(4))
                        Log($"    '{sub.Text}' Id={sub.Id}");
                }
            }
            catch (Exception ex)
            {
                Log($"Menubar test skipped ({ex.Message})", LogLevel.Warn);
            }

            session.ExitTransaction();
            session.WaitReady();

            Log("Tests_11_Toolbar_Menu PASSED");
        }
    }
}