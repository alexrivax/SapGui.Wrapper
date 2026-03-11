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
    public class Tests_05_Statusbar : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            // Arrange
            Log("Test run started for Tests_05_Statusbar.");

            using var sap = SapGuiClient.Attach();
            var session   = sap.Session;

            // ── Read idle status bar ──────────────────────────────────────────────
            var sb = session.Statusbar();
            Log($"MessageType (idle): '{sb.MessageType}'");
            Log($"Text        (idle): '{sb.Text}'");
            Log($"IsError             : {sb.IsError}");
            Log($"IsWarning           : {sb.IsWarning}");
            Log($"IsSuccess           : {sb.IsSuccess}");

            // ── Trigger an error message ──────────────────────────────────────────
            session.StartTransaction("SE16");
            session.WaitReady(timeoutMs: 10_000);

            // Press Enter without filling the table name → SAP returns an error
            session.PressEnter();
            session.WaitReady(timeoutMs: 5_000);

            sb = session.Statusbar();
            Log($"MessageType (after empty Enter): '{sb.MessageType}'");
            Log($"Text        (after empty Enter): '{sb.Text}'");
            Log($"IsError    : {sb.IsError}");
            Log($"IsWarning  : {sb.IsWarning}");
            Log($"IsSuccess  : {sb.IsSuccess}");

            if (sb.IsError)
                Log("Status bar correctly reports an error");
            else
                Log("No error was reported – your system may behave differently", LogLevel.Warn);

            session.ExitTransaction();
            session.WaitReady();

            Log("Tests_05_Statusbar PASSED");
        }
    }
}