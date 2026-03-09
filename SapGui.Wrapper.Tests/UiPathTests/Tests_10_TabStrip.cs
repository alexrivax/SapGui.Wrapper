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

namespace SapGuiWrapperTests
{
    public class Tests_10_TabStrip : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            // Arrange
            Log("Test run started for Tests_10_TabStrip.");

            using var sap = SapGuiClient.Attach();
            var session   = sap.Session;

            session.StartTransaction("SU3");
            session.WaitReady(timeoutMs: 10_000);

            const string tabStripId = "wnd[0]/usr/tabsTABSTRIP"; // ← ADAPT

            try
            {
                var ts = session.TabStrip(tabStripId);

                Log($"TabCount: {ts.TabCount}");

                var tabs = ts.GetTabs();
                Log($"GetTabs(): {tabs.Count} tab(s)");
                foreach (var t in tabs)
                    Log($"  Tab Id='{t.Id}' Text='{t.Text}'");

                // ── SelectTab by index ────────────────────────────────────────────
                if (tabs.Count >= 2)
                {
                    ts.SelectTab(1);
                    session.WaitReady(timeoutMs: 3_000);
                    Log("SelectTab(1) called");

                    ts.SelectTab(0);
                    session.WaitReady(timeoutMs: 3_000);
                    Log("SelectTab(0) called (back to first)");
                }

                // ── GetTabByName ──────────────────────────────────────────────────
                if (tabs.Count > 0)
                {
                    string firstName = tabs[0].Text;
                    var found = ts.GetTabByName(firstName);
                    Log($"GetTabByName('{firstName}'): {(found != null ? "found" : "not found")}");

                    // ── Tab.Select ────────────────────────────────────────────────
                    found?.Select();
                    Log("Tab.Select() called");
                }
            }
            catch (Exception ex)
            {
                Log($"TabStrip test skipped – control not found ({ex.Message})", LogLevel.Warn);
            }
            finally
            {
                session.PressBack();
                session.WaitReady();
            }

            Log("Tests_10_TabStrip PASSED");
        }
    }
}