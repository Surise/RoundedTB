using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace RoundedTB
{
    public class Background
    {
        // Just have a reference point for the Dispatcher
        public MainWindow mw;
        bool redrawOverride = false;
        int infrequentCount = 0;

        public Background()
        {
            mw = (MainWindow)Application.Current.MainWindow;
        }


        // Main method for the BackgroundWorker - runs indefinitely
        public void DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            if (worker == null)
            {
                return;
            }

            Log("Background worker started.");
            try
            {
                while (!worker.CancellationPending)
                {
                    try
                    {
                        RunIteration(worker);
                    }
                    catch (Exception ex)
                    {
                        Log($"Background iteration failed: {ex}");
                        ResetTaskbarsSafely();
                        if (WaitForCancellation(worker, 500))
                        {
                            break;
                        }
                    }
                    if (WaitForCancellation(worker, 100))
                    {
                        break;
                    }
                }
            }
            finally
            {
                e.Cancel = worker.CancellationPending;
                // A worker can stop because Explorer or UI Automation threw. Clear any
                // region/style left behind before the next worker or process exit.
                ResetTaskbarsSafely();
                Log(worker.CancellationPending ? "Background worker cancelled." : "Background worker stopped.");
            }
        }

        private void RunIteration(BackgroundWorker worker)
        {
            // Section for less frequent work (tray icon and second-instance requests).
            infrequentCount++;
            if (infrequentCount >= 10)
            {
                try
                {
                    List<IntPtr> windowList = Interaction.GetTopLevelWindows();
                    foreach (IntPtr hwnd in windowList)
                    {
                        StringBuilder windowClass = new StringBuilder(1024);
                        StringBuilder windowTitle = new StringBuilder(1024);
                        try
                        {
                            LocalPInvoke.GetClassName(hwnd, windowClass, 1024);
                            LocalPInvoke.GetWindowText(hwnd, windowTitle, 1024);
                            if (windowClass.ToString().StartsWith("HwndWrapper[RoundedTB", StringComparison.OrdinalIgnoreCase) &&
                                windowTitle.ToString() == "RoundedTB_SettingsRequest")
                            {
                                QueueUi(() =>
                                {
                                    if (mw.Visibility != Visibility.Visible)
                                    {
                                        mw.ShowMenuItem_Click(null, null);
                                    }
                                });
                                LocalPInvoke.SetWindowText(hwnd, "RoundedTB");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Unable to inspect window: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unable to inspect top-level windows: {ex.Message}");
                }

                QueueUi(() =>
                {
                    try
                    {
                        mw.TrayIconCheck();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Unable to update tray icon: {ex.Message}");
                    }
                });
                infrequentCount = 0;
            }

            Types.Settings settings = mw.activeSettings;
            if (settings == null)
            {
                return;
            }
            settings.IsCentred = Taskbar.CheckIfCentred();

            List<Types.Taskbar> taskbars = mw.taskbarDetails;
            if (taskbars == null || taskbars.Count == 0 || !Taskbar.TaskbarHandlesMatch(taskbars))
            {
                DisposeTaskbars(taskbars);
                taskbars = Taskbar.GenerateTaskbarInfo();
                Debug.WriteLine("Regenerating taskbar info");
            }

            for (int current = 0; current < taskbars.Count; current++)
            {
                if (worker.CancellationPending)
                {
                    break;
                }

                Types.Taskbar currentTaskbar = taskbars[current];
                bool appListValid = currentTaskbar?.AppListXaml?.IsAvailable == true ||
                    (currentTaskbar?.AppListHwnd != IntPtr.Zero && LocalPInvoke.IsWindow(currentTaskbar.AppListHwnd));
                bool trayValid = currentTaskbar?.TrayHwnd == IntPtr.Zero || LocalPInvoke.IsWindow(currentTaskbar.TrayHwnd);
                if (currentTaskbar == null || !LocalPInvoke.IsWindow(currentTaskbar.TaskbarHwnd) ||
                    !appListValid || !trayValid)
                {
                    DisposeTaskbars(taskbars);
                    taskbars = Taskbar.GenerateTaskbarInfo();
                    Debug.WriteLine("Regenerating taskbar info due to a missing handle");
                    break;
                }

                Types.Taskbar newTaskbar = Taskbar.GetQuickTaskbarRects(
                    currentTaskbar.TaskbarHwnd,
                    currentTaskbar.TrayHwnd,
                    currentTaskbar.AppListHwnd,
                    currentTaskbar.AppListXaml);
                if (newTaskbar == null ||
                    newTaskbar.TaskbarRect.Right <= newTaskbar.TaskbarRect.Left ||
                    newTaskbar.TaskbarRect.Bottom <= newTaskbar.TaskbarRect.Top)
                {
                    DisposeTaskbars(taskbars);
                    taskbars = Taskbar.GenerateTaskbarInfo();
                    Debug.WriteLine("Regenerating taskbar info due to invalid bounds");
                    break;
                }

                if (Taskbar.TaskbarShouldBeFilled(currentTaskbar.TaskbarHwnd, settings))
                {
                    if (!currentTaskbar.Ignored)
                    {
                        Taskbar.ResetTaskbar(currentTaskbar, settings);
                        currentTaskbar.Ignored = true;
                    }
                    continue;
                }

                UpdateHoverAndAutoHide(currentTaskbar, settings, worker);

                if (Taskbar.TaskbarRefreshRequired(currentTaskbar, newTaskbar, settings.IsDynamic) ||
                    currentTaskbar.Ignored || redrawOverride)
                {
                    currentTaskbar.Ignored = false;
                    int isFullTest = newTaskbar.TrayRect.Left - newTaskbar.AppListRect.Right;
                    Log($"Taskbar: {current} - AppList ends: {newTaskbar.AppListRect.Right} - Tray starts: {newTaskbar.TrayRect.Left} - Total gap: {isFullTest}");
                    bool simple = !settings.IsDynamic ||
                        (isFullTest <= currentTaskbar.ScaleFactor * 25 && isFullTest > 0 && newTaskbar.TrayRect.Left != 0);
                    bool dynamicValid = simple || Taskbar.CheckDynamicUpdateIsValid(currentTaskbar, newTaskbar);
                    bool applied = false;
                    if (dynamicValid)
                    {
                        // Only commit a rectangle after the validity checks pass. During an
                        // Explorer restart UI Automation can briefly report zero/overlapping bounds.
                        currentTaskbar.TaskbarRect = newTaskbar.TaskbarRect;
                        currentTaskbar.AppListRect = newTaskbar.AppListRect;
                        currentTaskbar.TrayRect = newTaskbar.TrayRect;
                        applied = simple
                            ? Taskbar.UpdateSimpleTaskbar(currentTaskbar, settings)
                            : Taskbar.UpdateDynamicTaskbar(currentTaskbar, settings);
                    }
                    if (applied)
                    {
                        Log($"Updated taskbar {current} {(simple ? "simply" : "dynamically")}");
                    }
                    else
                    {
                        // Keep retrying a failed region update; the taskbar may be
                        // rebuilding and become valid on the next pass.
                        currentTaskbar.Ignored = true;
                    }
                }
            }

            redrawOverride = false;
            mw.taskbarDetails = taskbars;
        }

        private void UpdateHoverAndAutoHide(Types.Taskbar taskbar, Types.Settings settings, BackgroundWorker worker)
        {
            if (settings.ShowSegmentsOnHover)
            {
                LocalPInvoke.RECT currentTrayRect = taskbar.TrayRect;
                LocalPInvoke.RECT currentWidgetsRect = taskbar.TaskbarRect;
                currentWidgetsRect.Right = Convert.ToInt32(currentWidgetsRect.Left + (168 * taskbar.ScaleFactor));
                if (currentTrayRect.Left != 0)
                {
                    LocalPInvoke.GetCursorPos(out LocalPInvoke.POINT msPt);
                    bool isHoveringOverTray = LocalPInvoke.PtInRect(ref currentTrayRect, msPt);
                    bool isHoveringOverWidgets = LocalPInvoke.PtInRect(ref currentWidgetsRect, msPt);
                    if (isHoveringOverTray != settings.ShowTray)
                    {
                        settings.ShowTray = isHoveringOverTray;
                        taskbar.Ignored = true;
                    }
                    if (isHoveringOverWidgets != settings.ShowWidgets)
                    {
                        settings.ShowWidgets = isHoveringOverWidgets;
                        taskbar.Ignored = true;
                    }
                }
            }

            LocalPInvoke.GetLayeredWindowAttributes(taskbar.TaskbarHwnd, out _, out byte opacity, out _);
            if (settings.AutoHide > 0)
            {
                LocalPInvoke.RECT currentRect = taskbar.TaskbarRect;
                LocalPInvoke.GetCursorPos(out LocalPInvoke.POINT mouse);
                if (taskbar.TaskbarHidden)
                {
                    currentRect.Top = currentRect.Bottom - 2;
                }
                bool hovering = LocalPInvoke.PtInRect(ref currentRect, mouse);
                if (hovering && opacity == 1)
                {
                    SetTaskbarOpacity(taskbar, new byte[] { 63, 127, 191, 255 }, worker);
                    taskbar.Ignored = true;
                    taskbar.TaskbarHidden = false;
                }
                else if (!hovering && opacity == 255)
                {
                    SetTaskbarOpacity(taskbar, new byte[] { 191, 127, 63, 1 }, worker);
                    int style = LocalPInvoke.GetWindowLong(taskbar.TaskbarHwnd, LocalPInvoke.GWL_EXSTYLE).ToInt32();
                    if ((style & LocalPInvoke.WS_EX_TRANSPARENT) != LocalPInvoke.WS_EX_TRANSPARENT)
                    {
                        LocalPInvoke.SetWindowLong(taskbar.TaskbarHwnd, LocalPInvoke.GWL_EXSTYLE, style | LocalPInvoke.WS_EX_TRANSPARENT);
                    }
                    taskbar.Ignored = true;
                    taskbar.TaskbarHidden = true;
                }
            }
            else if (opacity < 255)
            {
                int style = LocalPInvoke.GetWindowLong(taskbar.TaskbarHwnd, LocalPInvoke.GWL_EXSTYLE).ToInt32();
                if ((style & LocalPInvoke.WS_EX_TRANSPARENT) == LocalPInvoke.WS_EX_TRANSPARENT)
                {
                    LocalPInvoke.SetWindowLong(taskbar.TaskbarHwnd, LocalPInvoke.GWL_EXSTYLE, style & ~LocalPInvoke.WS_EX_TRANSPARENT);
                }
                SetTaskbarOpacity(taskbar, new byte[] { 63, 127, 191, 255 }, worker);
                taskbar.Ignored = true;
                taskbar.TaskbarHidden = false;
            }
        }

        private static void SetTaskbarOpacity(Types.Taskbar taskbar, byte[] values, BackgroundWorker worker)
        {
            foreach (byte value in values)
            {
                if (worker.CancellationPending || !LocalPInvoke.IsWindow(taskbar.TaskbarHwnd))
                {
                    return;
                }
                LocalPInvoke.SetLayeredWindowAttributes(taskbar.TaskbarHwnd, 0, value, LocalPInvoke.LWA_ALPHA);
                if (values.Length > 1 && WaitForCancellation(worker, 15))
                {
                    return;
                }
            }
        }

        private void QueueUi(Action action)
        {
            try
            {
                if (mw?.Dispatcher == null || mw.Dispatcher.HasShutdownStarted || mw.Dispatcher.HasShutdownFinished)
                {
                    return;
                }
                mw.Dispatcher.BeginInvoke(DispatcherPriority.Background, action);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to queue UI work: {ex.Message}");
            }
        }

        private void ResetTaskbarsSafely()
        {
            try
            {
                List<Types.Taskbar> taskbars = mw?.taskbarDetails;
                DisposeTaskbars(taskbars, true);
                if (mw != null && ReferenceEquals(mw.taskbarDetails, taskbars))
                {
                    mw.taskbarDetails = new List<Types.Taskbar>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to reset taskbars: {ex.Message}");
            }
        }

        private void DisposeTaskbars(List<Types.Taskbar> taskbars, bool reset = false)
        {
            taskbars ??= mw?.taskbarDetails;
            if (taskbars == null)
            {
                return;
            }
            foreach (Types.Taskbar taskbar in taskbars)
            {
                if (taskbar == null)
                {
                    continue;
                }
                if (reset && mw?.activeSettings != null)
                {
                    Taskbar.ResetTaskbar(taskbar, mw.activeSettings);
                }
                taskbar.Dispose();
            }
        }

        private void Log(string message)
        {
            try
            {
                mw?.interaction?.AddLog(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to write log: {ex.Message}");
            }
        }

        private static bool WaitForCancellation(BackgroundWorker worker, int milliseconds)
        {
            const int slice = 10;
            int elapsed = 0;
            while (elapsed < milliseconds)
            {
                if (worker.CancellationPending)
                {
                    return true;
                }
                Thread.Sleep(Math.Min(slice, milliseconds - elapsed));
                elapsed += slice;
            }
            return worker.CancellationPending;
        }
    }
}
