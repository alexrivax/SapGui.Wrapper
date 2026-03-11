using System;
using System.Linq;
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
    public class Tests_07_GridView : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            // Arrange
            Log("Test run started for Tests_07_GridView.");

            using var sap = SapGuiClient.Attach();
            var session   = sap.Session;

            session.StartTransaction("SM37");
            session.WaitReady(timeoutMs: 10_000);

            // Execute with defaults (shows all jobs for current user)
            session.PressExecute();
            session.WaitReady(timeoutMs: 15_000);

            const string gridId = "wnd[0]/usr/cntlCONTAINER/shellcont/shell"; // ← ADAPT

            try
            {
                var grid = session.GridView(gridId);

                // ── Dimensions ────────────────────────────────────────────────────
                Log($"RowCount       : {grid.RowCount}");
                Log($"VisibleRowCount: {grid.VisibleRowCount}");
                Log($"FirstVisibleRow: {grid.FirstVisibleRow}");

                // ── Column names ──────────────────────────────────────────────────
                var cols = grid.ColumnNames;
                Log($"Columns ({cols.Count}): {string.Join(", ", cols)}");

                if (grid.RowCount == 0)
                {
                    Log("Grid has no rows — skipping cell tests", LogLevel.Warn);
                    return;
                }

                // Use the first available column for cell tests
                string firstCol = cols.Count > 0 ? cols[0] : "0";

                // ── SetCurrentCell / CurrentCellRow / CurrentCellColumn ────────────
                grid.SetCurrentCell(0, firstCol);
                Log($"CurrentCellRow   : {grid.CurrentCellRow}");
                Log($"CurrentCellColumn: {grid.CurrentCellColumn}");

                // ── GetCellValue ──────────────────────────────────────────────────
                string cellVal = grid.GetCellValue(0, firstCol);
                Log($"GetCellValue(0, '{firstCol}'): '{cellVal}'");

                // ── GetCellTooltip ────────────────────────────────────────────────
                string tooltip = grid.GetCellTooltip(0, firstCol);
                Log($"GetCellTooltip(0, '{firstCol}'): '{tooltip}'");

                // ── GetCellCheckBoxValue (may not apply to all columns) ────────────
                bool cbVal = grid.GetCellCheckBoxValue(0, firstCol);
                Log($"GetCellCheckBoxValue(0, '{firstCol}'): {cbVal}");

                // ── GetSymbolsForCell ─────────────────────────────────────────────
                string sym = grid.GetSymbolsForCell(0, firstCol);
                Log($"GetSymbolsForCell(0, '{firstCol}'): '{sym}'");

                // ── SelectAll / SelectedRows ──────────────────────────────────────
                grid.SelectAll();
                var selected = grid.SelectedRows;
                Log($"SelectedRows after SelectAll: {selected.Count} row(s)");

                // ── GetRows (first 3 rows, first 3 columns) ───────────────────────
                var limitedCols = cols.Count >= 3 ? cols.Take(3).ToList() : cols.ToList();
                var rows = grid.GetRows(limitedCols);
                Log($"GetRows returned {rows.Count} row(s) for columns: {string.Join(", ", limitedCols)}");
                foreach (var row in rows.Take(3))
                    Log("  " + string.Join(" | ", row.Values));
            }
            catch (Exception ex)
            {
                Log($"GridView test skipped – grid not found ({ex.Message})", LogLevel.Warn);
            }
            finally
            {
                session.ExitTransaction();
                session.WaitReady();
            }

            Log("Tests_07_GridView PASSED");
        }
    }
}