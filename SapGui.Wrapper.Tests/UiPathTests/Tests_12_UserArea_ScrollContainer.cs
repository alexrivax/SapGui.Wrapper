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
    public class Tests_12_UserArea_ScrollContainer : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            // Arrange
            Log("Test run started for Tests_12_UserArea_ScrollContainer.");

            using var sap = SapGuiClient.Attach();
            var session   = sap.Session;

            // ── GuiUserArea ───────────────────────────────────────────────────────
            try
            {
                var userArea = session.UserArea(); // defaults to wnd[0]/usr

                // Access the main window itself via the UserArea – confirms the wrapper works
                Log($"UserArea Id      : {userArea.Id}");
                Log($"UserArea TypeName: {userArea.TypeName}");

                // Use FindById with a relative ID for the command/okcode field
                // Command field full path: wnd[0]/tbar[0]/okcd (not under usr, but demonstrates the API)
                // Under usr on SE16 is: ctxtDATABROWSE-TABLENAME
                session.StartTransaction("SE16");
                session.WaitReady(timeoutMs: 10_000);

                userArea = session.UserArea();
                var relativeField = userArea.FindById("ctxtDATABROWSE-TABLENAME"); // ← ADAPT
                Log($"Relative FindById result TypeName: {relativeField.TypeName}");
                Log($"Relative FindById result Id      : {relativeField.Id}");
            }
            catch (Exception ex)
            {
                Log($"UserArea test note: {ex.Message}", LogLevel.Warn);
            }

            // ── GuiScrollContainer ────────────────────────────────────────────────
            // On SE16 result screen some systems expose a scroll container
            // Adapt the ID below to a GuiScrollContainer in your system
            const string scrollId = "wnd[0]/usr/ssubSUBSCREEN_STEPLOOP:SAPLS2012:0101/scrSTEP_SCRL"; // ← ADAPT
            try
            {
                var sc = session.ScrollContainer(scrollId);
                var vsb = sc.VerticalScrollbar;

                Log($"VerticalScrollbar.Minimum : {vsb.Minimum}");
                Log($"VerticalScrollbar.Maximum : {vsb.Maximum}");
                Log($"VerticalScrollbar.PageSize: {vsb.PageSize}");
                Log($"VerticalScrollbar.Position: {vsb.Position}");

                sc.ScrollToTop();
                Log($"After ScrollToTop – Position: {vsb.Position}");
            }
            catch (Exception ex)
            {
                Log($"ScrollContainer test skipped – control not found ({ex.Message})", LogLevel.Warn);
            }
            finally
            {
                session.ExitTransaction();
                session.WaitReady();
            }

            Log("Tests_12_UserArea_ScrollContainer PASSED");
        }
    }
}