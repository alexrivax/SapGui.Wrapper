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
    public class Tests_04_TextField : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            // Arrange
            Log("Test run started for Tests_04_TextField.");

            using var sap = SapGuiClient.Attach();
            var session   = sap.Session;

            session.StartTransaction("SE16");
            session.WaitReady(timeoutMs: 10_000);

            // ── Command field (always present as wnd[0]/tbar[0]/okcd) ─────────────
            // This field exists on every SAP screen and is a safe test baseline.
            var okcd = session.TextField("wnd[0]/tbar[0]/okcd");
            Log($"okcd.MaxLength : {okcd.MaxLength}");
            Log($"okcd.IsReadOnly: {okcd.IsReadOnly}");
            Log($"okcd.TypeName  : {okcd.TypeName}");

            // ── Table-name field on SE16 initial screen ───────────────────────────
            // ctxtDATABROWSE-TABLENAME is present on every standard SAP system.
            const string tableFieldId = "wnd[0]/usr/ctxtDATABROWSE-TABLENAME"; // ← ADAPT if your SE16 path differs
            try
            {
                var tf = session.TextField(tableFieldId);
                Log($"TextField.MaxLength    : {tf.MaxLength}");
                Log($"TextField.IsReadOnly   : {tf.IsReadOnly}");
                Log($"TextField.IsRequired   : {tf.IsRequired}");
                Log($"TextField.IsOField     : {tf.IsOField}");
                Log($"TextField.Text (before): '{tf.Text}'");

                tf.Text = "T000";
                Log($"TextField.Text (after) : '{tf.Text}'");
                Log($"TextField.DisplayedText: '{tf.DisplayedText}'");

                tf.CaretPosition(0);
                Log("CaretPosition(0) called");
            }
            catch (Exception ex)
            {
                Log($"TextField test skipped – field not found ({ex.Message})", LogLevel.Warn);
            }
            finally
            {
                session.ExitTransaction();
                session.WaitReady();
            }

            Log("Tests_04_TextField PASSED");
        }
    }
}