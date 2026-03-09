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
    public class Tests_14_Events : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            // Arrange
            Log("Test run started for Tests_14_Events.");

            using var sap = SapGuiClient.Attach();
            var session   = sap.Session;

            int changeCount       = 0;
            int startRequestCount = 0;
            int endRequestCount   = 0;
            string lastFunction   = string.Empty;
            string lastMsgType    = string.Empty;

            session.Change += (_, e) =>
            {
                changeCount++;
                lastFunction = e.FunctionCode;
                lastMsgType  = e.MessageType;
                Log($"[Change] text='{e.Text}' func='{e.FunctionCode}' msgType='{e.MessageType}'");
            };

            session.StartRequest += (_, e) =>
            {
                startRequestCount++;
                Log($"[StartRequest] text='{e.Text}'");
            };

            session.EndRequest += (_, e) =>
            {
                endRequestCount++;
                Log($"[EndRequest] text='{e.Text}' func='{e.FunctionCode}' msgType='{e.MessageType}'");
            };

            session.AbapRuntimeError += (_, e) =>
                Log($"[AbapRuntimeError] message='{e.Message}'", LogLevel.Error);

            session.Destroy += (_, _) =>
                Log("[Destroy] session was closed", LogLevel.Warn);

            // ── Start monitoring ──────────────────────────────────────────────────
            session.StartMonitoring(pollMs: 300);
            Log("StartMonitoring() called");

            // ── Perform a round-trip to trigger events ────────────────────────────
            session.StartTransaction("SE16");
            session.WaitReady(timeoutMs: 10_000);

            session.ExitTransaction();
            session.WaitReady(timeoutMs: 5_000);

            // Give the polling thread / COM sink a moment to fire
            System.Threading.Thread.Sleep(1_000);

            // ── Stop monitoring ───────────────────────────────────────────────────
            session.StopMonitoring();
            Log("StopMonitoring() called");

            // ── Report results ────────────────────────────────────────────────────
            Log($"Change events fired      : {changeCount}");
            Log($"StartRequest events fired: {startRequestCount}");
            Log($"EndRequest events fired  : {endRequestCount}");
            Log($"Last FunctionCode        : '{lastFunction}'");
            Log($"Last MessageType         : '{lastMsgType}'");

            if (changeCount == 0)
                Log("WARNING: No Change events fired. Check if SAP scripting is enabled and a round-trip actually occurred.", LogLevel.Warn);

            Log("Tests_14_Events PASSED");
        }
    }
}