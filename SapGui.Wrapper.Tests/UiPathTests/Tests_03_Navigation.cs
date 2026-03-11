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
    public class Tests_03_Navigation : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            // Arrange
            Log("Test run started for Tests_03_Navigation.");

            using var sap = SapGuiClient.Attach();
            var session   = sap.Session;

            string startTCode = session.Info.Transaction;
            Log($"Starting transaction: {startTCode}");

            // ── StartTransaction ──────────────────────────────────────────────────
            // Use /n prefix so SAP navigates regardless of the current transaction.
            session.StartTransaction("/nSE16");
            // WaitForReadyState is stricter than WaitReady: it also verifies the
            // main window is reachable and adds a settle pause to catch double
            // busy pulses that a simple IsBusy poll can miss.
            session.WaitForReadyState(timeoutMs: 10_000, settleMs: 300);
            Log($"After StartTransaction('/nSE16') – TCode: {session.Info.Transaction}");

            // ── ElementExists: confirm a known SE16 field before interacting ──────
            // Use this instead of blindly calling TextField() — avoids a
            // SapComponentNotFoundException when the screen is still loading.
            const string tableNameField = "wnd[0]/usr/ctxtDATABROWSE-TABLENAME";
            bool appeared = session.ElementExists(tableNameField, timeoutMs: 8_000);
            Log($"SE16 table-name field appeared: {appeared}");

            // ── PressBack (F3) ────────────────────────────────────────────────────
            session.PressBack();
            session.WaitReady(timeoutMs: 5_000);
            Log($"After PressBack – TCode: {session.Info.Transaction}");

            // ── WaitUntilHidden: confirm SE16 field is gone after back ─────────────
            bool hidden = session.WaitUntilHidden(tableNameField, timeoutMs: 5_000);
            Log($"SE16 table-name field is now hidden: {hidden}");

            // ── WithRetry: wrap a navigation block that may hit timing issues ──────
            // WithRetry retries on SapComponentNotFoundException (slow screen load)
            // and TimeoutException (session still busy).  It never retries on
            // SapGuiNotFoundException — that is a fatal setup error.
            session.WithRetry(maxAttempts: 3, delayMs: 400).Run(() =>
            {
                session.StartTransaction("/nSE16");
                session.WaitForReadyState(timeoutMs: 10_000);
                // After the screen loads, assert the field exists as a cheap
                // confirmation that the transaction opened successfully.
                if (!session.ElementExists(tableNameField, timeoutMs: 5_000))
                    throw new SapComponentNotFoundException(tableNameField);
            });
            Log($"WithRetry block completed – TCode: {session.Info.Transaction}");

            // ── ExitTransaction ───────────────────────────────────────────────────
            session.ExitTransaction();
            session.WaitReady(timeoutMs: 5_000);
            Log($"After ExitTransaction – TCode: {session.Info.Transaction}");

            // ── SendVKey (Enter = 0) ──────────────────────────────────────────────
            session.SendVKey(0);
            session.WaitReady(timeoutMs: 5_000);
            Log("SendVKey(0) = Enter sent successfully");

            Log("Tests_03_Navigation PASSED");
        }
    }
}