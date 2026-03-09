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
    public class Tests_01_Session : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            // Arrange
            Log("Test run started for Tests_01_Session.");

            using var sap = SapGuiClient.Attach();
            var session   = sap.Session;
    
            // ── Session info ──────────────────────────────────────────────────────
            var info = session.Info;
            Log($"SystemName : {info.SystemName}");
            Log($"Client     : {info.Client}");
            Log($"User       : {info.User}");
            Log($"Language   : {info.Language}");
            Log($"Transaction: {info.Transaction}");
            Log($"Program    : {info.Program}");
            Log($"Screen     : {info.ScreenNumber}");
            Log($"AppServer  : {info.ApplicationServer}");
    
            // ── Connections / sessions ────────────────────────────────────────────
            var connections = sap.GetConnections();
            Log($"Connections: {connections.Count}");
    
            var firstConn = connections[0];
            Log($"Connection host: {firstConn.Host}");
    
            var sessions = firstConn.GetSessions();
            Log($"Sessions on conn[0]: {sessions.Count}");
    
            // ── IsBusy / WaitReady ────────────────────────────────────────────────
            Log($"IsBusy before WaitReady: {session.IsBusy}");
            session.WaitReady(timeoutMs: 5_000);
            Log($"IsBusy after WaitReady : {session.IsBusy}");
    
            // ── ActiveWindow ──────────────────────────────────────────────────────
            var active = session.ActiveWindow;
            Log($"ActiveWindow Id   : {active.Id}");
            Log($"ActiveWindow Title: {active.Title}");
    
            // ── Application ActiveSession shortcut ────────────────────────────────
            // ActiveSession is only reliable when the SAP window has OS focus;
            // fall back gracefully if the COM call throws.
            try
            {
                session.MainWindow().SetFocus();
                var appSession = sap.Application.ActiveSession;
                Log($"ActiveSession same Id: {appSession.Id == session.Id}");
            }
            catch (Exception ex)
            {
                Log($"ActiveSession unavailable (window may not have focus): {ex.Message}", LogLevel.Warn);
            }
    
            Log("Tests_01_Session PASSED");
        }
    }
}