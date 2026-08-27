using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Interop.UIAutomationClient;

namespace RoundedTB
{
    public class Types
    {
        public class Taskbar : IDisposable
        {
            public AppListXaml AppListXaml { get; set; }
            public IntPtr TaskbarHwnd { get; set; } // Handle to the taskbar
            public IntPtr TrayHwnd { get; set; } // Handle to the tray on the taskbar (if present)
            public IntPtr AppListHwnd { get; set; } // Handle to the list of open/pinned apps on the taskbar
            public LocalPInvoke.RECT TaskbarRect { get; set; } // Bounding box for the taskbar
            public LocalPInvoke.RECT TrayRect { get; set; }  // Bounding box for the tray (dynamic)
            public LocalPInvoke.RECT AppListRect { get; set; } // Bounding box for the list of pinned & open apps (dynamic)
            public IntPtr RecoveryHrgn { get; set; } // Pointer to the recovery region for any given taskbar. Defaults to IntPtr.Zero
            public double ScaleFactor { get; set; } // The scale factor of the monitor the taskbar is on
            public string TaskbarRes { get; set; } // Resolution of the taskbar as text
            public bool Ignored { get; set; } // Specifies if the taskbar should be ignored when applying changes
            public bool TaskbarHidden { get; set; } // Specifies if this taskbar is currently hidden by RTB
            public bool TrayHidden { get; set; } // Specifies if the tray is currently hidden by RTB on this taskbar
            public int AppListWidth { get; set; } // Specifies the width of the app list
            public TaskbarEffect TaskbarEffectWindow { get; set; } // Unused clone to apply effects to the taskbar

            public void Dispose()
            {
                AppListXaml?.Dispose();
            }
        }

        /// <summary>
        /// Cached UI Automation view of the Win11 23H2 taskbar frame. The old app-list HWND is
        /// retained as a fallback for Windows 10 through 22H2.
        /// </summary>
        public sealed class AppListXaml : IDisposable
        {
            private readonly IntPtr taskbarHwnd;
            private IUIAutomation automation;
            private IUIAutomationElement taskbarFrame;

            public AppListXaml(IntPtr taskbarHwnd)
            {
                this.taskbarHwnd = taskbarHwnd;

                try
                {
                    automation = new CUIAutomation();
                    taskbarFrame = FindTaskbarFrame(taskbarHwnd, automation);
                }
                catch
                {
                    Dispose();
                }
            }

            public bool IsAvailable => taskbarFrame != null;

            private static IUIAutomationElement FindTaskbarFrame(IntPtr taskbarHwnd, IUIAutomation uia)
            {
                IntPtr xamlBridge = LocalPInvoke.FindWindowExA(
                    taskbarHwnd,
                    IntPtr.Zero,
                    "Windows.UI.Composition.DesktopWindowContentBridge",
                    null);
                if (xamlBridge == IntPtr.Zero)
                {
                    return null;
                }

                IntPtr inputSite = LocalPInvoke.FindWindowExA(
                    xamlBridge,
                    IntPtr.Zero,
                    "Windows.UI.Input.InputSite.WindowClass",
                    null);
                if (inputSite == IntPtr.Zero)
                {
                    return null;
                }

                IUIAutomationElement root = null;
                IUIAutomationCondition condition = null;
                try
                {
                    root = uia.ElementFromHandle(inputSite);
                    if (root == null)
                    {
                        return null;
                    }

                    condition = uia.CreatePropertyCondition(
                        UIA_PropertyIds.UIA_AutomationIdPropertyId,
                        "TaskbarFrame");
                    return root.FindFirst(TreeScope.TreeScope_Children, condition);
                }
                finally
                {
                    ReleaseComObject(condition);
                    ReleaseComObject(root);
                }
            }

            public LocalPInvoke.RECT? GetWindowRect()
            {
                if (taskbarFrame == null || automation == null || !LocalPInvoke.IsWindow(taskbarHwnd))
                {
                    return null;
                }

                IUIAutomationElementArray children = null;
                IUIAutomationCondition condition = null;
                try
                {
                    condition = automation.CreateTrueCondition();
                    children = taskbarFrame.FindAll(TreeScope.TreeScope_Children, condition);
                    int childCount = children?.Length ?? 0;
                    if (childCount == 0)
                    {
                        return null;
                    }

                    bool hasBounds = false;
                    int left = 0;
                    int top = 0;
                    int right = 0;
                    int bottom = 0;

                    for (int i = 0; i < childCount; i++)
                    {
                        IUIAutomationElement child = null;
                        try
                        {
                            child = children.GetElement(i);
                            tagRECT bounds = child.CurrentBoundingRectangle;
                            if (bounds.right <= bounds.left || bounds.bottom <= bounds.top)
                            {
                                continue;
                            }

                            if (!hasBounds)
                            {
                                left = bounds.left;
                                top = bounds.top;
                                right = bounds.right;
                                bottom = bounds.bottom;
                                hasBounds = true;
                            }
                            else
                            {
                                left = Math.Min(left, bounds.left);
                                top = Math.Min(top, bounds.top);
                                right = Math.Max(right, bounds.right);
                                bottom = Math.Max(bottom, bounds.bottom);
                            }
                        }
                        finally
                        {
                            ReleaseComObject(child);
                        }
                    }

                    if (!hasBounds)
                    {
                        return null;
                    }

                    return new LocalPInvoke.RECT
                    {
                        Left = left,
                        Top = top,
                        Right = right,
                        Bottom = bottom
                    };
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unable to read the XAML taskbar bounds: {ex.Message}");
                    return null;
                }
                finally
                {
                    ReleaseComObject(condition);
                    ReleaseComObject(children);
                }
            }

            public void Dispose()
            {
                ReleaseComObject(taskbarFrame);
                taskbarFrame = null;
                ReleaseComObject(automation);
                automation = null;
            }

            private static void ReleaseComObject(object value)
            {
                if (value != null && Marshal.IsComObject(value))
                {
                    Marshal.ReleaseComObject(value);
                }
            }
        }

        public class Settings
        {
            public int Version {  get; set; }
            public SegmentSettings SimpleTaskbarLayout { get; set; }
            public SegmentSettings DynamicAppListLayout { get; set; }
            public SegmentSettings DynamicTrayLayout { get; set; }
            public SegmentSettings DynamicWidgetsLayout { get; set; }
            public bool IsDynamic { get; set; }
            public bool IsCentred { get; set; }
            public bool IsWindows11 { get; set; }
            public bool ShowTray { get; set; }
            public bool ShowWidgets { get; set; }
            public bool CompositionCompat { get; set; }
            public bool IsNotFirstLaunch { get; set; }
            public bool FillOnMaximise { get; set; }
            public bool FillOnTaskSwitch {  get; set; }
            public bool ShowSegmentsOnHover { get; set; }
            public int AutoHide { get; set; }
        }

        public class EffectiveRegion
        {
            public int CornerRadius { get; set; }
            public int Top { get; set; }
            public int Left { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public class SegmentSettings
        {
            public int CornerRadius { get; set; }
            public int MarginTop { get; set; }
            public int MarginLeft { get; set; }
            public int MarginBottom { get; set; }
            public int MarginRight { get; set; }
        }

        public enum TrayMode
        {
            Show = 0,
            Hide = 1,
            AutoHide = 2,
        }

        public enum CompositionMode
        {
            None = 0,
            TranslucentTB = 1,
            Legacy = 2,
        }

        public enum KeyModifier
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            WinKey = 8
        }
    }
}
