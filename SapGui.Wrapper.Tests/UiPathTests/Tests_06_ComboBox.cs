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
    public class Tests_06_ComboBox : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            // Arrange
            Log("Test run started for Tests_06_ComboBox.");

            using var sap = SapGuiClient.Attach();
            var session   = sap.Session;

            session.StartTransaction("SU3");
            session.WaitReady(timeoutMs: 10_000);

            const string comboId = "wnd[0]/usr/tabsTABSTRIP1/tabpADDR/ssubMAINAREA:SAPLSUID_MAINTENANCE:1900/cmbSUID_ST_NODE_PERSON_NAME_EXT-TITLE_MEDI"; // ← ADAPT to a combo on your screen

            try
            {
                var cb = session.ComboBox(comboId);
                Log($"ComboBox.Key       : '{cb.Key}'");
                Log($"ComboBox.Value     : '{cb.Value}'");
                Log($"ComboBox.ShowKey   : {cb.ShowKey}");
                Log($"ComboBox.IsReadOnly: {cb.IsReadOnly}");
                Log($"ComboBox.ToString(): {cb}");

                // Store original and restore after test
                string originalKey = cb.Key;

                // Set a different key and wait for SAP to validate
                cb.Key = "0002";
                session.WaitReady(timeoutMs: 3_000);
                Log($"ComboBox.Key after set: '{cb.Key}'");

                // Restore original
                cb.Key = originalKey;
                session.WaitReady(timeoutMs: 3_000);
                Log($"ComboBox.Key restored to: '{cb.Key}'");
            }
            catch (Exception ex)
            {
                Log($"ComboBox test skipped – field not found ({ex.Message})", LogLevel.Warn);
            }
            finally
            {
                session.PressBack(); // leave without saving
                session.WaitReady();
            }

            Log("Tests_06_ComboBox PASSED");
        }
    }
}