using IWshRuntimeLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Reflection;
using System.Windows.Threading;
using System.Windows.Interop;
using DesktopBridge;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using System.Diagnostics;
using Microsoft.Win32;
using System.Text;
using WPFUI;
using System.Windows.Forms;
using System.Windows.Media;
using System.Threading;

namespace RoundedTB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// 
    /// Many thanks to
    ///  - FloatingMilkshake
    ///  - cardin
    ///  - cleverActon0126
    ///  for your gracious donations! 💖
    ///  
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool isWindows11;
        public List<Types.Taskbar> taskbarDetails = new List<Types.Taskbar>();
        public bool shouldReallyDieNoReally = false;
        public string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "rtb.json");
        public string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "rtb.log");
        public Types.Settings activeSettings = new Types.Settings();
        public BackgroundWorker taskbarThread = new BackgroundWorker();
        public IntPtr hwndDesktopButton = IntPtr.Zero;
        public int lastDynDistance = 0;
        public int numberToForceRefresh = 0;
        public bool isCentred = false;
        public bool isAlreadyRunning = false;
        public Background background;
        public Interaction interaction;
        private HwndSource source;
        public int selectedSegment = 0; // 0 = Simple, 1 = AppList, 2 = Tray, 3 = Widgets
        public int version = -1;
        private bool closePending;
        private List<Types.Taskbar> shutdownTaskbars;
        private enum StartupLabel
        {
            RunAtStartup,
            Unavailable,
            Mandatory
        }

        private StartupLabel startupLabel = StartupLabel.RunAtStartup;
        /// <summary>
        /// Versions:
        /// -1: Canary
        ///  0: R3.0
        ///  1: P3.1B
        ///  2: R3.1
        ///  3: R4
        /// </summary>

        public MainWindow()
        {
            WPFUI.Background.Manager.Apply(WPFUI.Background.BackgroundType.Mica, this);

            InitializeComponent();


            // Check OS build, as behaviours rather-annoyingly differ between Windows 11 and Windows 10
            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            var buildNumber = registryKey?.GetValue("CurrentBuild")?.ToString();
            if (!int.TryParse(buildNumber, out int build) || build >= 21996)
            {
                isWindows11 = true;
            }
            else
            {
                isWindows11 = false;
                activeSettings.IsWindows11 = false;
            }

            // Initialise functions
            background = new Background();
            interaction = new Interaction();

            // Check if RoundedTB is already running, and if it is, do nothing.
            Process[] matchingProcesses = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            
            if (matchingProcesses.Length > 1)
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
                            windowTitle.ToString() == "RoundedTB")
                        {
                            LocalPInvoke.SetWindowText(hwnd, "RoundedTB_SettingsRequest");
                        }
                    }
                    catch (Exception) { }
                }
                shouldReallyDieNoReally = true;
                isAlreadyRunning = true;
                Close();
                return;
            }
            TrayIconCheck();

            if (IsRunningAsUWP())
            {
                #pragma warning disable CS4014
                StartupInit(true);
                configPath = Path.Combine(Windows.Storage.ApplicationData.Current.RoamingFolder.Path, "rtb.json");
                logPath = Path.Combine(Windows.Storage.ApplicationData.Current.RoamingFolder.Path, "rtb.log");
            }

            if (System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "RoundedTB.lnk")) && !IsRunningAsUWP())
            {
                StartupCheckBox.IsChecked = true;
                SetShowMenuItemHeader(false);
            }
            taskbarThread.WorkerSupportsCancellation = true;
            taskbarThread.WorkerReportsProgress = true;
            taskbarThread.DoWork +=background.DoWork;
            taskbarThread.RunWorkerCompleted += TaskbarThread_RunWorkerCompleted;

            // Load settings into memory/UI
            interaction.FileSystem();
            if (!IsRunningAsUWP())
            {
                interaction.AddLog($"RoundedTB started!");
            }
            else
            {
                interaction.AddLog($"RoundedTB started in UWP mode!");
            }
            activeSettings = interaction.ReadJSON();

            // Default settings
            if (activeSettings == null)
            {
                
                if (isWindows11) // Default settings for Windows 11
                {
                    activeSettings = new Types.Settings()
                    {
                        Language = Localization.Detect(),
                        SimpleTaskbarLayout = new Types.SegmentSettings{ CornerRadius = 7, MarginLeft = 3, MarginTop = 3, MarginRight = 3, MarginBottom = 3 },
                        DynamicAppListLayout = new Types.SegmentSettings { CornerRadius = 7, MarginLeft = 3, MarginTop = 3, MarginRight = 3, MarginBottom = 3 },
                        DynamicTrayLayout = new Types.SegmentSettings { CornerRadius = 7, MarginLeft = 3, MarginTop = 3, MarginRight = 3, MarginBottom = 3 },
                        DynamicWidgetsLayout = new Types.SegmentSettings { CornerRadius = 7, MarginLeft = 3, MarginTop = 3, MarginRight = 3, MarginBottom = 3 },
                        IsDynamic = false,
                        IsCentred = false,
                        IsWindows11 = true,
                        ShowTray = false,
                        ShowWidgets = false,
                        CompositionCompat = false,
                        IsNotFirstLaunch = false,
                        FillOnMaximise = true,
                        FillOnTaskSwitch = true,
                        ShowSegmentsOnHover = false,
                        AutoHide = 0
                    };
                }
                else // Default settings for Windows 10
                {
                    activeSettings = new Types.Settings()
                    {
                        Language = Localization.Detect(),
                        SimpleTaskbarLayout = new Types.SegmentSettings { CornerRadius = 16, MarginLeft = 2, MarginTop = 2, MarginRight = 2, MarginBottom = 2 },
                        DynamicAppListLayout = new Types.SegmentSettings { CornerRadius = 16, MarginLeft = 2, MarginTop = 2, MarginRight = 2, MarginBottom = 2 },
                        DynamicTrayLayout = new Types.SegmentSettings { CornerRadius = 16, MarginLeft = 2, MarginTop = 2, MarginRight = 2, MarginBottom = 2 },
                        DynamicWidgetsLayout = new Types.SegmentSettings { CornerRadius = 16, MarginLeft = 2, MarginTop = 2, MarginRight = 2, MarginBottom = 2 },
                        IsDynamic = false,
                        IsCentred = false,
                        IsWindows11 = false,
                        ShowTray = false,
                        ShowWidgets = false,
                        CompositionCompat = false,
                        IsNotFirstLaunch = false,
                        FillOnMaximise = true,
                        FillOnTaskSwitch = false,
                        ShowSegmentsOnHover = false,
                        AutoHide = 0
                    };
                }
            }

            // Preserve existing configurations while giving older files a language default.
            if (string.IsNullOrWhiteSpace(activeSettings.Language))
            {
                activeSettings.Language = Localization.Detect();
            }
            Localization.SetLanguage(activeSettings.Language);
            activeSettings.Language = Localization.IsChinese
                ? Localization.SimplifiedChinese
                : Localization.English;

            if (isWindows11)
            {
                activeSettings.IsWindows11 = true;
            }
            else
            {
                activeSettings.IsWindows11 = false;
            }

            ApplyLocalization();

            if (version != activeSettings.Version && version != -1)
            {
                activeSettings.IsNotFirstLaunch = false;
            }
            activeSettings.Version = version;


            interaction.AddLog($"Settings loaded:");
            interaction.AddLog(
                $"SimpleTaskbarLayout: {activeSettings.SimpleTaskbarLayout}\n" +
                $"DynamicAppListLayout: {activeSettings.DynamicAppListLayout}\n" +
                $"DynamicTrayLayout: {activeSettings.DynamicTrayLayout}\n" +
                $"DynamicWidgetsLayout: {activeSettings.DynamicWidgetsLayout}\n" +
                $"IsDynamic: {activeSettings.IsDynamic}\n" +
                $"IsCentred: {activeSettings.IsCentred}\n" +
                $"ShowTray: {activeSettings.ShowTray}\n" +
                $"ShowWidgets: {activeSettings.ShowWidgets}\n" +
                $"CompositionCompat: {activeSettings.CompositionCompat}\n" +
                $"IsNotFirstLaunch: {activeSettings.IsNotFirstLaunch}\n" +
                $"FillOnMaximise: {activeSettings.FillOnMaximise}\n" +
                $"FillOnTaskSwitch: {activeSettings.FillOnTaskSwitch}\n" +
                $"ShowTrayOnHover: {activeSettings.ShowSegmentsOnHover}\n"
                );

            // Checks if advanced margins are configured
            if (activeSettings.IsDynamic)
            {
                cornerRadiusInput.Text = activeSettings.DynamicAppListLayout.CornerRadius.ToString();
                cornerRadiusSlider.Value = activeSettings.DynamicAppListLayout.CornerRadius;
                mTopInput.Text = activeSettings.DynamicAppListLayout.MarginTop.ToString();
                mLeftInput.Text = activeSettings.DynamicAppListLayout.MarginLeft.ToString();
                mBottomInput.Text = activeSettings.DynamicAppListLayout.MarginBottom.ToString();
                mRightInput.Text = activeSettings.DynamicAppListLayout.MarginRight.ToString();

                selectedSegment = 1;
            }
            else
            {
                cornerRadiusInput.Text = activeSettings.SimpleTaskbarLayout.CornerRadius.ToString();
                cornerRadiusSlider.Value = activeSettings.SimpleTaskbarLayout.CornerRadius;
                mTopInput.Text = activeSettings.SimpleTaskbarLayout.MarginTop.ToString();
                mLeftInput.Text = activeSettings.SimpleTaskbarLayout.MarginLeft.ToString();
                mBottomInput.Text = activeSettings.SimpleTaskbarLayout.MarginBottom.ToString();
                mRightInput.Text = activeSettings.SimpleTaskbarLayout.MarginRight.ToString();

                selectedSegment = 0;
            }

            // Get whether or not taskbar is centred
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced"))
                {
                    if (key != null)
                    {
                        int val = (int)key.GetValue("TaskbarAl");
                        if (val == 1)
                        {
                            isCentred = true;
                        }
                        else
                        {
                            isCentred = false;
                        }
                        interaction.AddLog($"Taskbar centred? {isCentred}");
                    }
                }
            }
            catch (Exception aaaa)
            {
                interaction.AddLog(aaaa.Message);
            }
            if (!isWindows11)
            {
                activeSettings.IsCentred = false;
            }

            // Copy and apply settings to UI
            dynamicCheckBox.IsChecked = activeSettings.IsDynamic;
            centredCheckBox.IsChecked = activeSettings.IsCentred;
            showTrayCheckBox.IsChecked = activeSettings.ShowTray;
            showWidgetsCheckBox.IsChecked = activeSettings.ShowWidgets;
            fillMaximisedCheckBox.IsChecked = activeSettings.FillOnMaximise;
            fillAltTabCheckBox.IsChecked = activeSettings.FillOnTaskSwitch;
            showSegmentsOnHoverCheckBox.IsChecked = activeSettings.ShowSegmentsOnHover;
            compositionFixCheckBox.IsChecked = activeSettings.CompositionCompat;
            autoHideComboBox.SelectedIndex = activeSettings.AutoHide;
            taskbarDetails = Taskbar.GenerateTaskbarInfo();

            ApplyButton_Click(null, null);


            if (!activeSettings.FillOnMaximise)
            {
                activeSettings.FillOnTaskSwitch = false;
                fillAltTabCheckBox.IsEnabled = false;
            }

            //Showhide the split mode help button
            if (!isWindows11 && activeSettings.IsDynamic)
            {
                splitHelpButton.Visibility = Visibility.Visible;
            }
            else
            {
                splitHelpButton.Visibility = Visibility.Hidden;
            }

            if (activeSettings.IsNotFirstLaunch != true)
            {
                activeSettings.IsNotFirstLaunch = true;
                AboutWindow aw = new AboutWindow();
                aw.expander0.IsExpanded = true;
                aw.ShowDialog();
                try
                {
                    Visibility = Visibility.Visible;
                }
                catch (InvalidOperationException)
                {

                }
                SetShowMenuItemHeader(true);
            }

            AutoHide(true, taskbarDetails);

            UpdateUi();

        }

        public void ApplyLocalization()
        {
            if (activeSettings == null)
            {
                return;
            }

            Localization.SetLanguage(activeSettings.Language);
            Title = Localization.Text("RoundedTB", "RoundedTB");
            mainTitleBar.Title = Localization.Text("RoundedTB - Configuration", "RoundedTB - 配置");
            cornerRadiusLabel.Content = Localization.Text("Corner radius", "圆角半径");
            aboutButton.Content = Localization.Text("Help", "帮助");
            applyButton.Content = Localization.Text("Apply", "应用");
            dynamicCheckBox.Content = isWindows11
                ? Localization.Text("Dynamic mode", "动态模式")
                : Localization.Text("Split mode", "分栏模式");
            showTrayCheckBox.Content = Localization.Text("Show this segment", "显示此分区");
            showWidgetsCheckBox.Content = Localization.Text("Show this segment", "显示此分区");
            centredCheckBox.Content = Localization.Text("Centred taskbar?", "任务栏居中？");
            splitHelpButton.Content = Localization.Text("Click me!", "点击查看说明");
            showSegmentsOnHoverCheckBox.Content = Localization.Text(
                "Show segments only when hovered over with the mouse - PERFORMANCE ISSUES",
                "仅在鼠标悬停时显示分区（可能有性能问题）");
            fillMaximisedCheckBox.Content = Localization.Text(
                "When a window is maximised, restore the taskbar",
                "窗口最大化时恢复任务栏");
            fillAltTabCheckBox.Content = isWindows11
                ? Localization.Text(
                    "When alt+tab or win+tab is pressed, restore the taskbar",
                    "按 Alt+Tab 或 Win+Tab 时恢复任务栏")
                : Localization.Text("[Unavailable]", "[不可用]");
            compositionFixCheckBox.Content = Localization.Text(
                "Improve compatibility with TranslucentTB and other mods (may cause flickering)",
                "提高与 TranslucentTB 及其他修改工具的兼容性（可能导致闪烁）");
            mTopLabel.Content = Localization.Text("Top Margin", "上边距");
            mBottomLabel.Content = Localization.Text("Bottom Margin", "下边距");
            mLeftLabel.Content = Localization.Text("Left Margin", "左边距");
            mRightLabel.Content = Localization.Text("Right Margin", "右边距");
            diagramTitleLabel.Content = "RoundedTB";
            diagramSubtitleLabel.Content = Localization.Text(
                "To begin, select a taskbar segment below.",
                "请选择下方的任务栏分区开始设置。");
            autoHideLabel.Content = Localization.Text("Auto-hide", "自动隐藏");
            autoHideAlwaysShowItem.Content = Localization.Text("Always show", "始终显示");
            autoHideAlwaysHideItem.Content = Localization.Text("Always hide", "始终隐藏");
            autoHideUnavailableItem.Content = Localization.Text("[unavailable]", "[不可用]");

            UpdateStartupLabel();
            DebugMenuItem.Header = Localization.Text("Debug", "调试");
            LanguageMenuItem.Header = Localization.Text("Language", "语言");
            EnglishLanguageMenuItem.Header = Localization.Text("English", "英文");
            ChineseLanguageMenuItem.Header = Localization.Text("Simplified Chinese", "简体中文");
            CloseMenuItem.Header = Localization.Text("Close RoundedTB", "退出 RoundedTB");
            EnglishLanguageMenuItem.IsChecked = !Localization.IsChinese;
            ChineseLanguageMenuItem.IsChecked = Localization.IsChinese;
            SetShowMenuItemHeader(IsVisible);
        }

        private void UpdateStartupLabel()
        {
            StartupCheckBox.Content = startupLabel switch
            {
                StartupLabel.Unavailable => Localization.Text("Startup unavailable", "开机启动不可用"),
                StartupLabel.Mandatory => Localization.Text("Startup mandatory", "开机启动由系统强制启用"),
                _ => Localization.Text("Run at startup", "开机启动")
            };
        }

        private void SetShowMenuItemHeader(bool showWindow)
        {
            ShowMenuItem.Header = showWindow
                ? Localization.Text("Hide RoundedTB", "隐藏 RoundedTB")
                : Localization.Text("Show RoundedTB", "显示 RoundedTB");
        }

        private void EnglishLanguageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ChangeLanguage(Localization.English);
        }

        private void ChineseLanguageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ChangeLanguage(Localization.SimplifiedChinese);
        }

        private void ChangeLanguage(string language)
        {
            if (activeSettings == null)
            {
                return;
            }

            activeSettings.Language = language;
            Localization.SetLanguage(language);
            ApplyLocalization();
            interaction?.WriteJSON();
        }

        public void UpdateUi()
        {
            if (!activeSettings.ShowTray || activeSettings.ShowSegmentsOnHover)
            {
                trayRectStandIn.Opacity = 0.5;
            }
            else
            {
                trayRectStandIn.Opacity = 1;
            }

            if (!activeSettings.ShowWidgets || activeSettings.ShowSegmentsOnHover)
            {
                widgetsRectStandIn.Opacity = 0.5;
            }
            else
            {
                widgetsRectStandIn.Opacity = 1;
            }

            if (activeSettings.IsCentred && activeSettings.IsWindows11 && activeSettings.IsDynamic)
            {
                taskbarRectStandIn.Margin = new Thickness(126, 0, 126, 5);
                trayRectStandIn.Visibility = Visibility.Visible;
                widgetsRectStandIn.Visibility = Visibility.Visible;
            }
            else if (activeSettings.IsDynamic)
            {
                taskbarRectStandIn.Margin = new Thickness(5, 0, 247, 5);
                trayRectStandIn.Visibility = Visibility.Visible;
                widgetsRectStandIn.Visibility = Visibility.Hidden;
            }
            else
            {
                taskbarRectStandIn.Margin = new Thickness(5, 210, 5, 5);
                trayRectStandIn.Visibility = Visibility.Hidden;
                widgetsRectStandIn.Visibility = Visibility.Hidden;

            }
        }

        public void AutoHide(bool enabled, List<Types.Taskbar> taskbarDetails)
        {
            if (taskbarDetails == null || taskbarDetails.Count == 0)
            {
                return;
            }

            int workingHeight = Screen.PrimaryScreen.WorkingArea.Height;
            int boundsHeight = Screen.PrimaryScreen.Bounds.Height;
            int taskbarHeight = taskbarDetails[0].TaskbarRect.Bottom - taskbarDetails[0].TaskbarRect.Top;
            bool workAreaMisconfigured = false;

            if (boundsHeight - taskbarHeight > workingHeight)
            {
                workAreaMisconfigured = true;
            }

            if (activeSettings.AutoHide > 0 && enabled)
            {
                MonitorStuff.DisplayInfoCollection Displays = MonitorStuff.GetDisplays();

                foreach (MonitorStuff.DisplayInfo display in Displays)
                {
                    LocalPInvoke.RECT workArea = display.MonitorArea;
                    workArea.Bottom = workArea.Bottom - 2;
                    Interaction.SetWorkspace(workArea);
                }
                foreach (Types.Taskbar taskbar in taskbarDetails)
                {
                    LocalPInvoke.SetWindowPos(taskbar.TaskbarHwnd, new IntPtr(-1), 0, 0, 0, 0, LocalPInvoke.SetWindowPosFlags.IgnoreMove | LocalPInvoke.SetWindowPosFlags.IgnoreResize);
                    Taskbar.SetTaskbarState(LocalPInvoke.AppBarStates.AlwaysOnTop, taskbar.TaskbarHwnd);
                }
            }
            else if (!enabled)
            {
                foreach (Types.Taskbar taskbar in taskbarDetails)
                {
                    LocalPInvoke.SetWindowPos(taskbar.TaskbarHwnd, new IntPtr(-1), 0, 0, 0, 0, LocalPInvoke.SetWindowPosFlags.IgnoreMove | LocalPInvoke.SetWindowPosFlags.IgnoreResize);
                    if (workAreaMisconfigured)
                    {
                        Taskbar.SetTaskbarState(LocalPInvoke.AppBarStates.AutoHide, taskbar.TaskbarHwnd);
                        Taskbar.SetTaskbarState(LocalPInvoke.AppBarStates.AlwaysOnTop, taskbar.TaskbarHwnd);
                    }

                    MonitorStuff.DisplayInfoCollection Displays = MonitorStuff.GetDisplays();

                    foreach (MonitorStuff.DisplayInfo display in Displays)
                    {
                        taskbarHeight = taskbar.TaskbarRect.Bottom - taskbar.TaskbarRect.Top;
                        LocalPInvoke.RECT workArea = display.MonitorArea;
                        workArea.Bottom = workArea.Bottom - taskbarHeight;
                        Interaction.SetWorkspace(workArea);
                    }
                }
            }
        }

        public void TrayIconCheck()
        {
            
            Uri resLight = new("pack://application:,,,/res/traylight.ico");
            Uri resDark = new("pack://application:,,,/res/traydark.ico");
            WPFUI.Theme.Style style = WPFUI.Theme.Manager.GetSystemTheme();

            //if (style == WPFUI.Theme.Style.Light)
            //{
            //    mainTitleBar.NotifyIconImage = new System.Windows.Media.Imaging.BitmapImage(resLight);
            //}
            //else
            //{
            //    mainTitleBar.NotifyIconImage = new System.Windows.Media.Imaging.BitmapImage(resDark);
            //}
        }


        public async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            int mt = 0;
            int ml = 0;
            int mb = 0;
            int mr = 0;



            {
                if ((!int.TryParse(mTopInput.Text, out mt) && mTopInput.Text != string.Empty)
                || (!int.TryParse(mLeftInput.Text, out ml) && mLeftInput.Text != string.Empty)
                || (!int.TryParse(mBottomInput.Text, out mb) && mBottomInput.Text != string.Empty)
                || (!int.TryParse(mRightInput.Text, out mr) && mRightInput.Text != string.Empty))
                {
                    return;
                }
            }

            activeSettings.AutoHide = autoHideComboBox.SelectedIndex;
            activeSettings.IsDynamic = (bool)dynamicCheckBox.IsChecked;
            activeSettings.IsCentred = Taskbar.CheckIfCentred();
            activeSettings.ShowTray = (bool)showTrayCheckBox.IsChecked;
            activeSettings.ShowWidgets = (bool)showWidgetsCheckBox.IsChecked;
            activeSettings.CompositionCompat = (bool)compositionFixCheckBox.IsChecked;
            activeSettings.FillOnMaximise = (bool)fillMaximisedCheckBox.IsChecked;
            activeSettings.FillOnTaskSwitch = (bool)fillAltTabCheckBox.IsChecked;
            activeSettings.ShowSegmentsOnHover = (bool)showSegmentsOnHoverCheckBox.IsChecked;

            // Stop the previous worker without blocking the dispatcher. The old
            // implementation busy-waited here and could deadlock on Dispatcher.Invoke.
            if (!await StopBackgroundWorkerAsync())
            {
                return;
            }

            // Cancellation resets the old taskbar objects so a transient failure
            // cannot leave stale UI Automation handles behind. Build a fresh
            // snapshot before applying the new settings.
            if (taskbarDetails == null || taskbarDetails.Count == 0)
            {
                try
                {
                    taskbarDetails = Taskbar.GenerateTaskbarInfo();
                }
                catch (Exception ex)
                {
                    interaction.AddLog($"Unable to rediscover taskbars while applying settings: {ex}");
                    taskbarDetails = new List<Types.Taskbar>();
                }
            }

            try
            {
                foreach (Types.Taskbar taskbar in taskbarDetails)
                {
                    int isFullTest = taskbar.TrayRect.Left - taskbar.AppListRect.Right;
                    if (!activeSettings.IsDynamic || (isFullTest <= taskbar.ScaleFactor * 25 && isFullTest > 0 && taskbar.TrayRect.Left != 0))
                    {
                        Taskbar.UpdateSimpleTaskbar(taskbar, activeSettings);
                    }
                    else
                    {
                        Taskbar.UpdateDynamicTaskbar(taskbar, activeSettings);
                    }
                }
            }
            catch (InvalidOperationException aaaa)
            {
                interaction.AddLog(aaaa.Message);
            }


            if (activeSettings.AutoHide < 1)
            {
                AutoHide(false, taskbarDetails);
            }
            else
            {
                AutoHide(true, taskbarDetails);
            }
            interaction.WriteJSON();
            TrayIconCheck();
            UpdateUi();

            if (!taskbarThread.IsBusy && !closePending)
            {
                taskbarThread.RunWorkerAsync((mt, ml, mb, mr, 0));
            }

        }

        private async Task<bool> StopBackgroundWorkerAsync()
        {
            if (!taskbarThread.IsBusy)
            {
                return true;
            }

            taskbarThread.CancelAsync();
            DateTime deadline = DateTime.UtcNow.AddSeconds(2);
            while (taskbarThread.IsBusy)
            {
                await Task.Delay(25);
                if (DateTime.UtcNow >= deadline)
                {
                    interaction.AddLog("Timed out waiting for the background worker to stop; settings were not applied.");
                    return false;
                }
            }
            return true;
        }

        private void TaskbarThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!closePending)
            {
                return;
            }

            closePending = false;
            Dispatcher.BeginInvoke(new Action(Close), DispatcherPriority.Background);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (shouldReallyDieNoReally == false)
            {
                e.Cancel = true;
                Visibility = Visibility.Hidden;
                SetShowMenuItemHeader(false);
            }
            else
            {


                if (taskbarThread.IsBusy)
                {
                    shutdownTaskbars = taskbarDetails == null
                        ? new List<Types.Taskbar>()
                        : new List<Types.Taskbar>(taskbarDetails);
                    closePending = true;
                    try
                    {
                        taskbarThread.CancelAsync();
                    }
                    catch (Exception aaaa)
                    {
                        interaction.AddLog(aaaa.Message);
                    }
                    e.Cancel = true;
                    return;
                }

                FinalizeClose();
            }
            if (!isAlreadyRunning)
            {
                interaction.WriteJSON();
            }
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Close any popups - leave main window for now
            for (int windowCount = App.Current.Windows.Count - 1; windowCount >= 0; windowCount--)
            {
                App.Current.Windows[windowCount].Close();
            }
            
            shouldReallyDieNoReally = true;

            Close();
        }

        public void ShowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsVisible == false)
            {
                Visibility = Visibility.Visible;
                SetShowMenuItemHeader(true);
            }
            else
            {
                // Close any popups - leave main window for now
                for (int windowCount = App.Current.Windows.Count - 1; windowCount >= 0; windowCount--)
                {
                    App.Current.Windows[windowCount].Close();
                }
                Visibility = Visibility.Hidden;
                SetShowMenuItemHeader(false);
            }
        }

        private async void Startup_Clicked(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Startup toggled");
            if (IsRunningAsUWP())
            {
                await StartupToggle();
                await StartupInit(false);
            }
            else
            {
                if (System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "RoundedTB.lnk")))
                {
                    System.IO.File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "RoundedTB.lnk"));
                }
                else
                {
                    EnableStartup();
                }
            }
        }

        public void EnableStartup()
        {
            try
            {
                string shortcutFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                if (!Directory.Exists(shortcutFolder))
                {
                    Directory.CreateDirectory(shortcutFolder);
                }
                WshShell shellClass = new WshShell();
                string rtbStartupLink = Path.Combine(shortcutFolder, "RoundedTB.lnk");
                IWshShortcut shortcut = (IWshShortcut)shellClass.CreateShortcut(rtbStartupLink);
                shortcut.TargetPath = Environment.GetCommandLineArgs()[0];
                shortcut.IconLocation = Environment.GetCommandLineArgs()[0];
                shortcut.Arguments = "";
                shortcut.Description = "Start RoundedTB";
                shortcut.Save();
            }
            catch (Exception)
            {
            }
        }

        async Task StartupToggle()
        {
            StartupTask startupTask = await StartupTask.GetAsync("RTB"); // Pass the task ID you specified in the appxmanifest file
            switch (startupTask.State)
            {
                case StartupTaskState.Disabled:
                    StartupTaskState newState = await startupTask.RequestEnableAsync();
                    StartupCheckBox.IsEnabled = true;
                    break;

                case StartupTaskState.DisabledByUser:
                    StartupCheckBox.IsEnabled = false;
                    break;

                case StartupTaskState.EnabledByPolicy:
                    StartupCheckBox.IsEnabled = false;
                    break;

                case StartupTaskState.DisabledByPolicy:
                    StartupCheckBox.IsEnabled = false;
                    break;

                case StartupTaskState.Enabled:
                    startupTask.Disable();
                    StartupCheckBox.IsEnabled = true;
                    break;
            }
        }

        async Task StartupInit(bool clean)
        {
            StartupTask startupTask = await StartupTask.GetAsync("RTB");
            switch (startupTask.State)
            {
                case StartupTaskState.Disabled:
                    StartupCheckBox.IsChecked = false;
                    StartupCheckBox.IsEnabled = true;
                    if (clean)
                    {
                        Visibility = Visibility.Visible;
                        SetShowMenuItemHeader(true);
                    }
                    startupLabel = StartupLabel.RunAtStartup;
                    UpdateStartupLabel();
                    break;

                case StartupTaskState.DisabledByUser:
                    StartupCheckBox.IsChecked = false;
                    StartupCheckBox.IsEnabled = false;
                    if (clean)
                    {
                        Visibility = Visibility.Visible;
                        SetShowMenuItemHeader(true);
                    }
                    startupLabel = StartupLabel.Unavailable;
                    UpdateStartupLabel();
                    break;

                case StartupTaskState.EnabledByPolicy:
                    StartupCheckBox.IsChecked = true;
                    StartupCheckBox.IsEnabled = false;
                    if (clean)
                    {
                        Visibility = Visibility.Hidden;
                        SetShowMenuItemHeader(false);
                    }
                    startupLabel = StartupLabel.Mandatory;
                    UpdateStartupLabel();
                    break;

                case StartupTaskState.DisabledByPolicy:
                    StartupCheckBox.IsChecked = false;
                    StartupCheckBox.IsEnabled = false;
                    if (clean)
                    {
                        Visibility = Visibility.Visible;
                        SetShowMenuItemHeader(true);
                    }
                    startupLabel = StartupLabel.Unavailable;
                    UpdateStartupLabel();
                    break;

                case StartupTaskState.Enabled:
                    StartupCheckBox.IsChecked = true;
                    StartupCheckBox.IsEnabled = true;
                    if (clean)
                    {
                        Visibility = Visibility.Hidden;
                        SetShowMenuItemHeader(false);
                    }
                    startupLabel = StartupLabel.RunAtStartup;
                    UpdateStartupLabel();
                    break;
            }
        }

        // Checks if running as a UWP app
        public bool IsRunningAsUWP()
        {
            try
            {
                Helpers helpers = new Helpers();
                return helpers.IsRunningAsUwp();
            }
            catch (Exception)
            {
                return false;
            }

        }

        private void DebugMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (taskbarDetails == null || taskbarDetails.Count == 0 ||
                taskbarDetails[0].TaskbarHwnd == IntPtr.Zero)
            {
                return;
            }

            IntPtr hwndNext = LocalPInvoke.FindWindowExA(taskbarDetails[0].TaskbarHwnd, IntPtr.Zero, "Start", null);
            List<IntPtr> floatingMilkshakesBitsOfTaskbar = new List<IntPtr>();
            while (hwndNext != IntPtr.Zero && !floatingMilkshakesBitsOfTaskbar.Contains(hwndNext))
            {
                floatingMilkshakesBitsOfTaskbar.Add(hwndNext);
                hwndNext = LocalPInvoke.FindWindowExA(taskbarDetails[0].TaskbarHwnd, hwndNext, null, null);
            }
            foreach (IntPtr hwnd in floatingMilkshakesBitsOfTaskbar)
            {
                LocalPInvoke.GetWindowRect(hwnd, out LocalPInvoke.RECT rect);
                LocalPInvoke.MoveWindow(hwnd, rect.Left + 50, rect.Top, (rect.Right + 50) - (rect.Left + 50), rect.Bottom - rect.Top, true);
            }
        }

        private async void ContextMenu_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsRunningAsUWP())
            {
                await StartupInit(false);
            }
        }

        private void dynamicCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            centredCheckBox.IsEnabled = true;
            showSegmentsOnHoverCheckBox.IsEnabled = true;
            showSegmentsOnHoverCheckBox.IsChecked = false;
            showTrayCheckBox.IsEnabled = true;
            showTrayCheckBox.IsChecked = true;
            
            if (!isWindows11)
            {
                splitHelpButton.Visibility = Visibility.Visible;
                if (Opacity > 0.5)
                {
                    splitHelpButton_Click(null, null);
                }
            }

        }

        private void dynamicCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {

            centredCheckBox.IsEnabled = false;
            centredCheckBox.IsChecked = false;
            showSegmentsOnHoverCheckBox.IsEnabled = false;
            showSegmentsOnHoverCheckBox.IsChecked = false;
            showTrayCheckBox.IsEnabled = false;
            showTrayCheckBox.IsChecked = false;
            
            if (!isWindows11)
            {
                splitHelpButton.Visibility = Visibility.Hidden;
            }
        }

        private void cornerRadiusSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            int check = Convert.ToInt32(Math.Round(cornerRadiusSlider.Value));
            cornerRadiusInput.Text = check.ToString();

            switch (selectedSegment)
            {
                default:
                    break;

                case 0:
                    activeSettings.SimpleTaskbarLayout.CornerRadius = check;
                    break;

                case 1:
                    activeSettings.DynamicAppListLayout.CornerRadius = check;
                    break;

                case 2:
                    activeSettings.DynamicTrayLayout.CornerRadius = check;
                    break;

                case 3:
                    activeSettings.DynamicWidgetsLayout.CornerRadius = check;
                    break;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            Debug.WriteLine("AAAAA");
            base.OnSourceInitialized(e);


            IntPtr handle = new WindowInteropHelper(this).Handle;
            source = HwndSource.FromHwnd(handle);
            source.AddHook(interaction.HwndHook);
            bool wtf = LocalPInvoke.RegisterHotKey(handle, 9000, 0x8, 0x71);
            Debug.WriteLine("KEY: " + wtf);
            Debug.WriteLine(handle);
            Debug.WriteLine((int)Types.KeyModifier.WinKey);
            Debug.WriteLine(System.Windows.Forms.Keys.J.GetHashCode());
            Visibility = Visibility.Hidden;
            Opacity = 1;
            SetShowMenuItemHeader(false);
        }

        private void FinalizeClose()
        {
            try
            {
                List<Types.Taskbar> taskbars = shutdownTaskbars ?? taskbarDetails;
                if (taskbars != null)
                {
                    foreach (Types.Taskbar taskbar in taskbars)
                    {
                        Taskbar.ResetTaskbar(taskbar, activeSettings);
                    }
                    if (activeSettings.AutoHide > 0 && taskbars.Count > 0)
                    {
                        AutoHide(false, taskbars);
                    }
                }
            }
            catch (Exception aaaa)
            {
                interaction.AddLog($"Taskbar structure changed on exit:\n{aaaa.Message}");
            }
            shutdownTaskbars = null;
            interaction.AddLog("Exiting RoundedTB.");
        }

        private void splitHelpButton_Click(object sender, RoutedEventArgs e)
        {
            Infobox ib = new Infobox();
            ib.Title = Localization.Text("RoundedTB - Split mode configuration", "RoundedTB - 分栏模式配置");
            ib.titleBlock.Text = Localization.Text("How to use Split Mode", "如何使用分栏模式");
            ib.bodyBlock.Text = Localization.Text(
                "Split mode has a couple of limitations and requires a small amount of setup to get working properly.\n\nLimitations:\n1) Split mode doesn't resize itself automatically. This feature will be coming to RoundedTB for Windows 10 in the future.\n2) Toolbars are not compatible with split mode currently, and will need to be disabled apart from one (more on that in a moment).\n3) Split mode only works when the taskbar is horizontal at the top or bottom of the screen.\n\nSetup:\n1) Right-click the taskbar and disable \"Lock the taskbar\".\n2) Right-click it again and turn off any existing toolbars.\n3) Right-click a third time, select Toolbars > Desktop.\n4) Use the small || handle to resize the taskbar as you please.",
                "分栏模式有一些限制，需要完成少量设置才能正常工作。\n\n限制：\n1) 分栏模式不会自动调整大小，未来版本会改进 Windows 10 支持。\n2) 分栏模式暂不兼容工具栏，除一个工具栏外请关闭其他工具栏。\n3) 分栏模式仅支持任务栏位于屏幕顶部或底部。\n\n设置：\n1) 右键任务栏，取消勾选“锁定任务栏”。\n2) 再次右键任务栏，关闭现有工具栏。\n3) 右键任务栏，选择“工具栏 > 桌面”。\n4) 使用小的 || 手柄按需调整任务栏大小。");
            ib.ShowDialog();
        }

        private void compositionFixCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (Opacity > 0.01)
            {
                Infobox ib = new Infobox();
                ib.Height = 450;
                ib.Title = Localization.Text("RoundedTB - TranslucentTB compatibility", "RoundedTB - TranslucentTB 兼容性");
                ib.titleBlock.Text = Localization.Text("Compatibility with TranslucentTB", "与 TranslucentTB 兼容");
                ib.bodyBlock.Text = Localization.Text(
                    "\nTranslucentTB is a utility that allows you to customise the opacity, blur and colour of the taskbar seamlessly with significantly finer control than other tools. Enable this option to allow RoundedTB and TranslucentTB to work together.\n\nThis is necessary due to a bug in Windows (it's not the fault of RoundedTB or TranslucentTB), and you might encounter some minor flickering when the taskbar \"updates\" (changes size, roundness or position). This is usually pretty minimal and many people use RoundedTB and TranslucentTB in tandem without complaint, but if it bothers you then I recommend sticking with either RoundedTB or TranslucentTB until a better solution is available.\n\nRegardless though, go show TranslucentTB some love! It's the OG Windows 10 aesthetic taskbar mod, the first one on the Microsoft Store and the project that inspired me to make RoundedTB. Plus, the dev is pretty awesome 💖",
                    "\nTranslucentTB 可无缝自定义任务栏的不透明度、模糊效果和颜色，控制精度高于其他工具。启用此选项后，RoundedTB 可以与 TranslucentTB 协同工作。\n\n这是因为 Windows 中的一个错误（并非 RoundedTB 或 TranslucentTB 的问题）。任务栏“更新”（大小、圆角或位置变化）时可能出现轻微闪烁。通常影响很小；如果仍然困扰你，可以暂时只使用 RoundedTB 或 TranslucentTB。\n\n也请支持 TranslucentTB！它是 Windows 10 任务栏美化工具的先驱，也是启发 RoundedTB 的项目。");
                ib.ShowDialog();
            }
        }

        private void aboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aw = new AboutWindow();
            aw.ShowDialog();
        }

        private void fillMaximisedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (isWindows11)
            {
                fillAltTabCheckBox.IsEnabled = true;
            }
        }

        private void fillMaximisedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            fillAltTabCheckBox.IsEnabled = false;
            fillAltTabCheckBox.IsChecked = false;

        }

        private void showSegmentsOnHoverCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            showTrayCheckBox.IsEnabled = false;
            showTrayCheckBox.IsChecked = false;

            showWidgetsCheckBox.IsEnabled = false;
            showWidgetsCheckBox.IsChecked = false;
        }

        private void showSegmentsOnHoverCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            showTrayCheckBox.IsEnabled = true;
            showTrayCheckBox.IsChecked = true;

            showWidgetsCheckBox.IsEnabled = true;
            showWidgetsCheckBox.IsChecked = true;
        }

        private void taskbarRectStandIn_Click(object sender, RoutedEventArgs e)
        {
            taskbarRectStandIn.Appearance = WPFUI.Common.Appearance.Primary;
            trayRectStandIn.Appearance = WPFUI.Common.Appearance.Secondary;
            widgetsRectStandIn.Appearance = WPFUI.Common.Appearance.Secondary;
            dynamicCheckBox.Visibility = Visibility.Visible;
            showTrayCheckBox.Visibility = Visibility.Hidden;
            showWidgetsCheckBox.Visibility = Visibility.Hidden;

            if (activeSettings.IsDynamic)
            {
                selectedSegment = 1;

                cornerRadiusInput.Text = activeSettings.DynamicAppListLayout.CornerRadius.ToString();
                cornerRadiusSlider.Value = activeSettings.DynamicAppListLayout.CornerRadius;
                mTopInput.Text = activeSettings.DynamicAppListLayout.MarginTop.ToString();
                mLeftInput.Text = activeSettings.DynamicAppListLayout.MarginLeft.ToString();
                mBottomInput.Text = activeSettings.DynamicAppListLayout.MarginBottom.ToString();
                mRightInput.Text = activeSettings.DynamicAppListLayout.MarginRight.ToString();
            }
            else
            {
                selectedSegment = 0;

                cornerRadiusInput.Text = activeSettings.SimpleTaskbarLayout.CornerRadius.ToString();
                cornerRadiusSlider.Value = activeSettings.SimpleTaskbarLayout.CornerRadius;
                mTopInput.Text = activeSettings.SimpleTaskbarLayout.MarginTop.ToString();
                mLeftInput.Text = activeSettings.SimpleTaskbarLayout.MarginLeft.ToString();
                mBottomInput.Text = activeSettings.SimpleTaskbarLayout.MarginBottom.ToString();
                mRightInput.Text = activeSettings.SimpleTaskbarLayout.MarginRight.ToString();
            }
        }

        private void trayRectStandIn_Click(object sender, RoutedEventArgs e)
        {
            taskbarRectStandIn.Appearance = WPFUI.Common.Appearance.Secondary;
            trayRectStandIn.Appearance = WPFUI.Common.Appearance.Primary;
            widgetsRectStandIn.Appearance = WPFUI.Common.Appearance.Secondary;
            dynamicCheckBox.Visibility = Visibility.Hidden;
            showTrayCheckBox.Visibility = Visibility.Visible;
            showWidgetsCheckBox.Visibility = Visibility.Hidden;

            selectedSegment = 2;

            cornerRadiusInput.Text = activeSettings.DynamicTrayLayout.CornerRadius.ToString();
            cornerRadiusSlider.Value = activeSettings.DynamicTrayLayout.CornerRadius;
            mTopInput.Text = activeSettings.DynamicTrayLayout.MarginTop.ToString();
            mLeftInput.Text = activeSettings.DynamicTrayLayout.MarginLeft.ToString();
            mBottomInput.Text = activeSettings.DynamicTrayLayout.MarginBottom.ToString();
            mRightInput.Text = activeSettings.DynamicTrayLayout.MarginRight.ToString();
        }

        private void widgetsRectStandIn_Click(object sender, RoutedEventArgs e)
        {
            taskbarRectStandIn.Appearance = WPFUI.Common.Appearance.Secondary;
            trayRectStandIn.Appearance = WPFUI.Common.Appearance.Secondary;
            widgetsRectStandIn.Appearance = WPFUI.Common.Appearance.Primary;
            dynamicCheckBox.Visibility = Visibility.Hidden;
            showTrayCheckBox.Visibility = Visibility.Hidden;
            showWidgetsCheckBox.Visibility = Visibility.Visible;

            selectedSegment = 3;

            cornerRadiusInput.Text = activeSettings.DynamicWidgetsLayout.CornerRadius.ToString();
            cornerRadiusSlider.Value = activeSettings.DynamicWidgetsLayout.CornerRadius;
            mTopInput.Text = activeSettings.DynamicWidgetsLayout.MarginTop.ToString();
            mLeftInput.Text = activeSettings.DynamicWidgetsLayout.MarginLeft.ToString();
            mBottomInput.Text = activeSettings.DynamicWidgetsLayout.MarginBottom.ToString();
            mRightInput.Text = activeSettings.DynamicWidgetsLayout.MarginRight.ToString();
        }

        private void mTopInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(mTopInput.Text, out int check) && mTopInput.Text != string.Empty)
            {
                switch (selectedSegment)
                {
                    default:
                        break;

                    case 0:
                        activeSettings.SimpleTaskbarLayout.MarginTop = check;
                        break;

                    case 1:
                        activeSettings.DynamicAppListLayout.MarginTop = check;
                        break;

                    case 2:
                        activeSettings.DynamicTrayLayout.MarginTop = check;
                        break;

                    case 3:
                        activeSettings.DynamicWidgetsLayout.MarginTop = check;
                        break;
                }
            }
        }

        private void mBottomInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(mBottomInput.Text, out int check) && mBottomInput.Text != string.Empty)
            {
                switch (selectedSegment)
                {
                    default:
                        break;

                    case 0:
                        activeSettings.SimpleTaskbarLayout.MarginBottom = check;
                        break;

                    case 1:
                        activeSettings.DynamicAppListLayout.MarginBottom = check;
                        break;

                    case 2:
                        activeSettings.DynamicTrayLayout.MarginBottom = check;
                        break;

                    case 3:
                        activeSettings.DynamicWidgetsLayout.MarginBottom = check;
                        break;
                }
            }
        }

        private void mLeftInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(mLeftInput.Text, out int check) && mLeftInput.Text != string.Empty)
            {
                switch (selectedSegment)
                {
                    default:
                        break;

                    case 0:
                        activeSettings.SimpleTaskbarLayout.MarginLeft = check;
                        break;

                    case 1:
                        activeSettings.DynamicAppListLayout.MarginLeft = check;
                        break;

                    case 2:
                        activeSettings.DynamicTrayLayout.MarginLeft = check;
                        break;

                    case 3:
                        activeSettings.DynamicWidgetsLayout.MarginLeft = check;
                        break;
                }
            }
        }

        private void mRightInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(mRightInput.Text, out int check) && mRightInput.Text != string.Empty)
            {
                switch (selectedSegment)
                {
                    default:
                        break;

                    case 0:
                        activeSettings.SimpleTaskbarLayout.MarginRight = check;
                        break;

                    case 1:
                        activeSettings.DynamicAppListLayout.MarginRight = check;
                        break;

                    case 2:
                        activeSettings.DynamicTrayLayout.MarginRight = check;
                        break;

                    case 3:
                        activeSettings.DynamicWidgetsLayout.MarginRight = check;
                        break;
                }
            }
        }

        private void cornerRadiusInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(cornerRadiusInput.Text, out int check) && cornerRadiusInput.Text != string.Empty)
            {
                switch (selectedSegment)
                {
                    default:
                        break;

                    case 0:
                        activeSettings.SimpleTaskbarLayout.CornerRadius = check;
                        break;

                    case 1:
                        activeSettings.DynamicAppListLayout.CornerRadius = check;
                        break;

                    case 2:
                        activeSettings.DynamicTrayLayout.CornerRadius = check;
                        break;

                    case 3:
                        activeSettings.DynamicWidgetsLayout.CornerRadius = check;
                        break;
                }

                cornerRadiusSlider.Value = check;
            }
        }

        private void cornerRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            cornerRadiusInput.Text = Math.Round(cornerRadiusSlider.Value).ToString();
        }
    }
}
