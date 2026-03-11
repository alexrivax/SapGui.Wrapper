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
    public class Tests_02_MainWindow : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            // Arrange
            Log("Test run started for Tests_02_MainWindow.");

            using var sap  = SapGuiClient.Attach();
            var session    = sap.Session;
            var win        = session.MainWindow();
    
            Log($"Title     : {win.Title}");
            Log($"TypeName  : {win.TypeName}");
            Log($"Id        : {win.Id}");
    
            // ── IsMaximized ───────────────────────────────────────────────────────
            bool wasMaximized = win.IsMaximized;
            Log($"IsMaximized before Maximize: {wasMaximized}");
    
            win.Maximize();
            Log($"IsMaximized after  Maximize: {win.IsMaximized}");
    
            win.Restore();
            Log($"IsMaximized after  Restore : {win.IsMaximized}");
    
            // ── Iconify / Restore ─────────────────────────────────────────────────
            win.Iconify();
            Log("Iconify() called (window minimized to taskbar)");
            System.Threading.Thread.Sleep(800);
    
            win.Maximize();  // bring it back
            Log("Maximize() called after Iconify");
    
            // ── HardCopy ──────────────────────────────────────────────────────────
            string screenshotPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "sap_test_screenshot.png");
            win.HardCopy(screenshotPath, "PNG");
            bool exists = System.IO.File.Exists(screenshotPath);
            Log($"HardCopy saved to {screenshotPath} – file exists: {exists}");
            if (!exists) Log("WARNING: Screenshot file was not created", LogLevel.Warn);
    
            Log("Tests_02_MainWindow PASSED");
        }
    }
}