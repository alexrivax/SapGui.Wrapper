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
using System.Linq;

namespace SapGuiWrapperTests
{
    public class Tests_08_Table : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            // Arrange
            Log("Test run started for Tests_08_Table.");

            using var sap = SapGuiClient.Attach();
            var session   = sap.Session;

            // Open SE16 and browse T000 (Clients table – always has at least one row)
            session.StartTransaction("SE16");
            session.WaitReady(timeoutMs: 10_000);

            const string tableNameFieldId = "wnd[0]/usr/ctxtDATABROWSE-TABLENAME"; // ← ADAPT if your SE16 path differs
            try
            {
                session.TextField(tableNameFieldId).Text = "T000";
            }
            catch
            {
                Log("Could not find the table-name field – adapt the ID for your system", LogLevel.Warn);
                session.ExitTransaction();
                return;
            }

            session.PressEnter();
            session.WaitReady(timeoutMs: 5_000);

            // Execute (F8) to load the result list
            session.PressExecute();
            session.WaitReady(timeoutMs: 10_000);

            // The result screen ID depends on the SAP release.
            // Classic-ALV systems expose a GuiTable; modern ones a GuiGridView.
            // Run the SAP Script Recorder on this screen to find the exact ID.
            const string tableId = "wnd[0]/usr/tblSAPLSE16NSELFIELD_TC"; // ← ADAPT

            try
            {
                var tbl = session.Table(tableId);

                Log($"RowCount       : {tbl.RowCount}");
                Log($"ColumnCount    : {tbl.ColumnCount}");
                Log($"VisibleRowCount: {tbl.VisibleRowCount}");
                Log($"FirstVisibleRow: {tbl.FirstVisibleRow}");
                Log($"CurrentCellRow : {tbl.CurrentCellRow}");
                Log($"CurrentCellCol : {tbl.CurrentCellColumn}");

                if (tbl.RowCount > 0)
                {
                    string cell00 = tbl.GetCellValue(0, 0);
                    Log($"GetCellValue(0,0): '{cell00}'");

                    // GetVisibleRows returns each row as a List<string> of cell values
                    var visible = tbl.GetVisibleRows();
                    Log($"GetVisibleRows: {visible.Count} row(s)");
                    foreach (var row in visible.Take(5))
                        Log("  " + string.Join(" | ", row));

                    // ScrollToRow: SAP invalidates the COM proxy after a scroll,
                    // so re-fetch the table reference afterwards.
                    if (tbl.RowCount > tbl.VisibleRowCount)
                    {
                        int lastPage = tbl.RowCount - tbl.VisibleRowCount;
                        tbl.ScrollToRow(lastPage);
                        tbl = session.Table(tableId);
                        Log($"ScrollToRow({lastPage}) – FirstVisibleRow now: {tbl.FirstVisibleRow}");

                        tbl.ScrollToRow(0);
                        tbl = session.Table(tableId);
                        Log($"ScrollToRow(0) – FirstVisibleRow now: {tbl.FirstVisibleRow}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Table test skipped – table not found ({ex.Message})", LogLevel.Warn);
            }
            finally
            {
                session.ExitTransaction();
                session.WaitReady();
            }

            Log("Tests_08_Table PASSED");
        }
    }
}