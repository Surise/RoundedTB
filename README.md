# RoundedTB_Fix2026

![RoundedTB logo](PackagingProject/Images/Wide310x150Logo.scale-400.png)

为 Windows 任务栏添加边距、圆角和分段。
Add margins, rounded corners and segments to your Windows taskbar.

## 项目来源 / Project origin

本项目源代码来源于作者的开源项目 [RoundedTB/RoundedTB](https://github.com/RoundedTB/RoundedTB)。
This project is based on the author's open-source [RoundedTB/RoundedTB](https://github.com/RoundedTB/RoundedTB) repository.

原项目发布距今已有较长时间。随着 Windows 11 任务栏架构更新，尤其是 Windows 11 23H2 及更高版本，使用动态/分栏布局时任务栏应用列表可能只显示前几个应用，无法完整显示。本仓库在原作者开源代码的基础上进行了兼容性和稳定性修复。
The upstream project is several years old. After the Windows 11 taskbar architecture changed, especially in Windows 11 23H2 and later, dynamic or split layouts could show only a few taskbar applications instead of the complete list. This repository applies compatibility and stability fixes on top of the author's open-source code.

## 修复内容 / Fixes

- **Windows 11 23H2+ 应用列表**：通过 UI Automation 查找 `TaskbarFrame` 并聚合完整边界，解决动态模式下任务栏应用显示不完整的问题；不可用时保留旧 HWND 方式作为兼容回退。
  **Windows 11 23H2+ app list**: finds `TaskbarFrame` through UI Automation and combines the complete bounds, fixing incomplete app display in dynamic mode; the legacy HWND path remains as a compatibility fallback.
- **多显示器和副任务栏**：分别处理主任务栏、副任务栏以及多个 `WorkerW` 容器。
  **Multiple monitors and secondary taskbars**: handles primary and secondary taskbars independently, including multiple `WorkerW` containers.
- **Explorer 重启和任务栏重建**：检测窗口句柄失效，释放旧 UI Automation 对象，重新发现任务栏并自动应用当前布局。
  **Explorer restarts and taskbar rebuilds**: detects invalid handles, releases stale UI Automation objects, rediscovers taskbars and reapplies the current layout.
- **后台稳定性**：捕获后台异常并重试；失败时保留有效状态，发生致命失败或取消时恢复任务栏，避免长时间运行后任务栏消失。
  **Background stability**: catches and retries transient failures; preserves the last valid state and restores the taskbar after fatal failure or cancellation, preventing the taskbar from disappearing during long-running use.
- **线程和 UI 死锁**：取消操作不再同步阻塞 UI 线程，后台等待支持取消。
  **Threading and UI deadlocks**: cancellation no longer synchronously blocks the UI thread, and background waits can be cancelled.
- **GDI 资源泄漏**：正确释放动态布局使用的 `HRGN` 区域句柄，降低长时间运行时 GDI 句柄持续增长的风险。
  **GDI resource leaks**: releases `HRGN` region handles created for dynamic layouts, reducing GDI handle growth during long sessions.
- **TranslucentTB 通信**：使用有超时的窗口消息，避免兼容程序无响应时拖死 RoundedTB。
  **TranslucentTB communication**: uses a timed window message so an unresponsive compatibility process cannot hang RoundedTB.
- **配置和启动崩溃**：兼容旧版 `rtb.json`，启动时自动迁移缺失字段；修复重复启动检测，并使用临时文件替换降低配置损坏风险。
  **Configuration and startup crashes**: migrates older `rtb.json` files with missing fields, fixes duplicate-launch detection, and writes through a temporary replacement file to reduce corruption risk.

## 新增功能 / New features

- **中英文界面**：支持 English 和简体中文，首次启动会根据 Windows 显示语言选择默认语言。
  **Bilingual interface**: supports English and Simplified Chinese; the first launch follows the Windows display language.
- **运行时切换语言**：从托盘菜单的 `Language / 语言` 子菜单即时切换，选择会保存并在下次启动恢复。
  **Runtime language switching**: switch immediately from the tray menu's `Language / 语言` submenu; the choice is saved and restored on the next launch.
- **长时间运行保护**：后台健康监测、失败重试、Explorer 自动恢复、可取消等待和日志轮换。
  **Long-running protection**: background health monitoring, failure retries, automatic Explorer recovery, cancellable waits and log rotation.
- **安全配置保存**：配置写入采用临时文件和替换策略，减少进程中断造成的配置丢失。
  **Safer configuration saves**: writes use a temporary file and replacement strategy to reduce loss when the process is interrupted.


## 使用 / Usage

RoundedTB 常驻系统托盘。启动后配置窗口默认隐藏，不会占用任务栏；右键托盘图标并选择 **Show RoundedTB / 显示 RoundedTB** 打开设置。
RoundedTB runs in the system tray. The configuration window is hidden after startup so it does not occupy the taskbar; right-click the tray icon and choose **Show RoundedTB** to open settings.

### 语言 / Language

首次启动时，如果 Windows 显示语言为中文，程序默认使用简体中文，否则使用英文。可以随时从托盘菜单选择 **Language / 语言** > **Simplified Chinese / 简体中文** 或 **English / 英文**。
On first launch, Simplified Chinese is selected when the Windows display language is Chinese; otherwise English is used. At any time choose **Language** > **Simplified Chinese** or **English** from the tray menu.

### 基本选项 / Basic options

- **Margin / 边距**：从任务栏每一侧移除指定像素，形成可见且可点击穿透的边距。
  Removes the specified number of pixels from each side of the taskbar, creating a visible margin that clicks can pass through.
- **Corner Radius / 圆角半径**：调整任务栏圆角大小。
  Adjusts how round the taskbar corners are.

### 高级选项 / Advanced options

- **Independent Margins / 独立边距**：点击边距输入框旁的 `...`，分别设置上、下、左、右边距；也可以使用负值将任务栏贴到显示器边缘。
  Click the `...` button beside the margin box to set top, bottom, left and right margins independently; negative values can attach the taskbar to a screen edge.
- **Dynamic Mode (Windows 11) / 动态模式（Windows 11）**：根据应用图标数量自动调整任务栏宽度，类似 macOS Dock。
  Automatically resizes the taskbar to match the number of app icons, similar to the macOS Dock.
- **Split Mode (Windows 10) / 分栏模式（Windows 10）**：将应用区与系统托盘分开并允许手动调整大小。Windows 10 需要先完成下方的设置。
  Separates the app area from the system tray and lets you resize it manually. Windows 10 requires the setup described below.
- **Show System Tray / 显示系统托盘**：控制动态/分栏模式是否显示系统托盘和时钟，也可以按 `Win`+`F2` 切换。
  Controls whether the system tray and clock are shown in dynamic/split mode; press `Win`+`F2` to toggle it.
- **TranslucentTB Compatibility / TranslucentTB 兼容性**：为 TranslucentTB 提供实验性刷新兼容。需要 TranslucentTB 2021.5，切换时可能出现轻微闪烁。
  Provides experimental refresh compatibility with TranslucentTB. It requires TranslucentTB 2021.5 and may flicker slightly while switching.
- **About RoundedTB / 关于 RoundedTB**：查看版本、配置文件和日志文件。
  Shows version information and provides access to the configuration and log files.

### Windows 11 23H2 及更高版本 / Windows 11 23H2 and later

Windows 11 将任务栏应用列表移到了 XAML 界面。动态模式现在读取完整的 `TaskbarFrame` 边界，因此所有固定和正在运行的应用都能参与布局，而不只是前几个按钮。主任务栏和副任务栏分别处理；Explorer 重启后程序会自动等待新句柄并恢复布局。
Windows 11 moved the taskbar app list into a XAML surface. Dynamic mode now reads the complete `TaskbarFrame` bounds, so all pinned and running apps participate in the layout instead of only the first few buttons. Primary and secondary taskbars are handled independently, and the layout is restored after Explorer restarts.

### 长时间运行 / Long-running use

后台监控会在 UI Automation 或 Explorer 暂时不可用时重试，并在更新失败时保留有效状态；只有发生不可恢复错误时才执行完整恢复。动态区域句柄会在每次更新后释放，日志也会限制大小并轮换，适合长时间驻留托盘。
The background monitor retries temporary UI Automation or Explorer failures and keeps the last valid state when an update fails; a full reset is reserved for unrecoverable errors. Dynamic region handles are released after each update, and logs are size-limited and rotated for long tray sessions.

## 已知问题 / Known issues

- 自动隐藏仍处于实验阶段，配合 TranslucentTB 或动态/分栏模式时可能闪烁（[upstream #36](https://github.com/torchgm/RoundedTB/issues/36)）。
  Auto-hide is still experimental and may flicker with TranslucentTB or dynamic/split mode ([upstream #36](https://github.com/torchgm/RoundedTB/issues/36)).
- Windows 限制导致圆角无法抗锯齿（[upstream #4](https://github.com/torchgm/RoundedTB/issues/4)）。
  Rounded corners are not antialiased because of a Windows limitation ([upstream #4](https://github.com/torchgm/RoundedTB/issues/4)).
- 如果从未更改过任务栏对齐方式，动态模式可能无法隐藏左侧；先切换到左对齐再切回居中可以绕过此问题（[upstream #98](https://github.com/torchgm/RoundedTB/issues/98)）。
  Dynamic mode may not hide the left side if taskbar alignment has never changed; switch to Left and back to Center as a workaround ([upstream #98](https://github.com/torchgm/RoundedTB/issues/98)).
- 动态模式和分栏模式最适合显示器顶部或底部的水平任务栏。
  Dynamic and split mode work best with a horizontal taskbar at the top or bottom of a display.
- Windows 10 分栏模式只支持主任务栏，副任务栏不会分栏。
  Windows 10 split mode supports only the primary taskbar; secondary taskbars are not split.
- 偶尔需要移动窗口或短暂切换任务栏对齐方式，才能让动态尺寸重新计算。
  Occasionally moving a window or briefly changing taskbar alignment may be needed to recalculate the dynamic size.
- 除 TranslucentTB 2021.5 外的任务栏修改工具不保证兼容。
  Compatibility with taskbar modification tools other than TranslucentTB 2021.5 is not guaranteed.

## 其他信息 / Other information

RoundedTB 不会永久修改系统；启用托盘菜单中的开机启动后，它只会随 Windows 启动。如果 Explorer 被手动重启，请保持 RoundedTB 运行，程序会等待新的任务栏句柄并恢复布局。若任务栏出现严重异常，可按 `Ctrl`+`Shift`+`Esc` 打开任务管理器，结束 RoundedTB 后重启 Explorer。
RoundedTB makes no permanent system changes. If startup is enabled from the tray menu, it only runs when Windows starts. If Explorer is restarted manually, leave RoundedTB running so it can wait for new taskbar handles and restore the layout. If the taskbar becomes unusable, press `Ctrl`+`Shift`+`Esc`, end RoundedTB in Task Manager and restart Explorer.

## Windows 10 分栏模式设置 / Configuring split mode on Windows 10

### 限制 / Limitations

- 分栏模式不会自动调整大小。
  Split mode does not resize itself automatically.
- 工具栏与分栏模式不完全兼容，除用于标记空白区域的工具栏外应关闭其他工具栏。
  Toolbars are not fully compatible; disable all except the one used to mark the empty area.
- 分栏模式只适用于主显示器顶部或底部的水平任务栏。
  Split mode works only with a horizontal taskbar at the top or bottom of the primary monitor.

### 设置步骤 / Setup

1. 右键任务栏，关闭“锁定任务栏”。
   Right-click the taskbar and disable **Lock the taskbar**.
2. 再次右键任务栏，关闭现有工具栏。
   Right-click it again and turn off existing toolbars.
3. 第三次右键任务栏，选择“工具栏” > “桌面”。
   Right-click it a third time and select **Toolbars** > **Desktop**.
4. 使用出现的 `||` 小手柄调整任务栏大小。
   Use the small `||` handle to resize the taskbar.

原项目的设置视频仍可通过以下链接访问：
The upstream setup video is available at:

[Windows 10 split mode setup video](https://user-images.githubusercontent.com/31840547/134795022-1312d011-40f2-4641-8c8d-3d6c0e752747.mp4)

## 自动构建发布 / Automated Release

向 GitHub 推送以 `v` 开头的标签（例如 `v1.0.0`）会自动触发 Windows 2022 构建，生成自包含的 `win-x64` Release ZIP，并将其发布到对应的 GitHub Release。也可以在 Actions 页面手动运行工作流进行构建验证；手动运行不会创建正式 Release。
Pushing a tag that starts with `v` (for example, `v1.0.0`) automatically builds the self-contained `win-x64` Release ZIP on Windows 2022 and publishes it to the matching GitHub Release. The workflow can also be run manually from the Actions page for build verification; manual runs do not create a release.


## 许可证 / License

本项目及修改内容遵循 [GNU GPL v3](LICENSE)。请保留原项目版权和来源声明，并在分发修改版本时注明修改内容。
This project and its modifications are distributed under the [GNU GPL v3](LICENSE). Preserve the upstream copyright and attribution notices, and identify changes when distributing a modified version.

## 反馈 / Feedback

欢迎通过 Issue 报告问题或提交改进建议。原项目的讨论也可以在其 [Discord server](https://discord.gg/wYQJd8VGSB) 中进行。
Bug reports and improvement suggestions are welcome through Issues. Upstream discussions are also available in the [Discord server](https://discord.gg/wYQJd8VGSB).
