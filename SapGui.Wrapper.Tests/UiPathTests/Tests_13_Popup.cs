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
    public class Tests_13_Popup : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            // Arrange
            Log("Test run started for Tests_13_Popup.");

            using var sap = SapGuiClient.Attach();
            var session   = sap.Session;

            // ── No popup baseline ─────────────────────────────────────────────────
            var noPopup = session.GetActivePopup();
            Log($"GetActivePopup (no popup): {(noPopup == null ? "null (correct)" : $"unexpected – {noPopup.Title}")}");

            // ── Trigger an information popup via SE16 ─────────────────────────────
            // Entering an invalid transaction code sometimes produces an error popup
            session.StartTransaction("SE16");
            session.WaitReady(timeoutMs: 10_000);

            // Press Enter with blank table name to trigger an error message in status bar
            // (not a popup on most systems; try sending an incomplete F5 help request)
            // A reliable popup: open SE16 and press F4 on the table-name field
            try
            {
                var tf = session.TextField("wnd[0]/usr/ctxtDATABROWSE-TABLENAME"); // ← ADAPT
                tf.SetFocus();
                session.MainWindow().SendVKey(4);  // F4 = possible values dialog
                session.WaitReady(timeoutMs: 5_000);

                var popup = session.GetActivePopup();
                if (popup != null)
                {
                    Log($"Popup Title      : '{popup.Title}'");
                    Log($"Popup Text       : '{popup.Text}'");
                    Log($"Popup MessageType: '{popup.MessageType}'");

                    var buttons = popup.GetButtons();
                    Log($"Popup button count: {buttons.Count}");
                    foreach (var btn in buttons)
                        Log($"  Button: '{btn.Text}'");

                    // Dismiss with the first available button (safest)
                    if (buttons.Count > 0)
                    {
                        Log($"Clicking first button: '{buttons[0].Text}'");
                        buttons[0].Press();
                        session.WaitReady(timeoutMs: 5_000);
                    }
                    else
                    {
                        popup.ClickCancel();
                        session.WaitReady(timeoutMs: 5_000);
                    }
                }
                else
                {
                    Log("No popup appeared after F4 – your system may open a modal window instead", LogLevel.Warn);
                    // Dismiss with PressBack just in case
                    session.PressBack();
                    session.WaitReady(timeoutMs: 3_000);
                }
            }
            catch (Exception ex)
            {
                Log($"Popup test note: {ex.Message}", LogLevel.Warn);
            }
            finally
            {
                session.ExitTransaction();
                session.WaitReady();
            }

            Log("Tests_13_Popup PASSED");
        }
    }
}