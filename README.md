![RoundedTB](https://cdn.discordapp.com/attachments/272509873479221249/891555515799318568/unknown.png)

# RoundedTB
#### Add margins, rounded corners and segments to your taskbars!

![image](https://user-images.githubusercontent.com/31840547/134795141-76349eaf-12da-40f8-b2a0-d7b7c268d152.png)


## How do I get it?
The easiest way to download RoundedTB is from the [Microsoft Store](https://www.microsoft.com/store/productId/9MTFTXSJ9M7F). You can also download the latest version from the Releases tab, unzip it and run `RoundedTB.exe`. If you're a madman, you can compile it yourself or check out the latest [Canary build](https://nightly.link/torchgm/RoundedTB/workflows/ci/master/rtb-artifacts.zip) (note these can be very unfinished, buggy and unstable).

## To use

RoundedTB is a tray-resident application. After it starts, the configuration
window is hidden so it does not occupy the taskbar. Right-click the RoundedTB
tray icon and choose **Show RoundedTB** to open the settings window.

### Language / 语言

The first launch follows the Windows display language when it is Chinese;
otherwise English is used. To switch at any time, open the tray menu and choose
**Language** > **Simplified Chinese** or **English**. The choice is saved in
`%LOCALAPPDATA%\rtb.json` and is restored on the next launch.

### Basic options
The simplest way to use RoundedTB is by simply entering a margin and corner radius.
 - **Margin** - controls how many pixels to remove from each side of the taskbar, creating a margin around it that you can see and click through.
 -  **Corner Radius** - adjusts how round the corners of the taskbar should be.

### Advanced options
The advanced options allow for further customisation, at the cost of some user-friendliness.
- **Independent Margins** - in the advanced settings, a <kbd>...</kbd> button appears on the margin box. Click it to enable independent margins, which allow you to specify the margin for each side of the taskbar. You can also use negative values to hide the rounded corners for some sides, allowing you to "attach" the taskbar to different sides of the monitor.
- **Dynamic Mode (Windows 11)** - dynamic mode automatically resizes the taskbars to accommodate the number of icons in it, making the taskbar behave similarly to macOS' Dock.
- **Split Mode (Windows 10)** - split mode is a simplified version of dynamic mode for Windows 10. Due to a more limited taskbar, dynamically resizing the taskbar isn't possible. However after some setup, split mode allows you to separate the taskbar from the system tray and resize it at will. I admit it's certainly not as cool as dynamic mode but for now it's better than nothing 🥺. For info on setting up, see the bottom of this readme.
- **Show System Tray** - this toggles whether or not the system tray, clock etc. is displayed in dynamic/split mode. It can be toggled at any time by pressing <kbd>Win</kbd>+<kbd>F2</kbd>.
- **TranslucentTB Compatibility** - due to a bug in Windows, apps that alter the composition of the taskbar don't allow RoundedTB's changes to show up automatically. Whilst I'm currently not aware of a fix, I've worked closely with [Sylveon](https://github.com/sylveon) to enable some level of compatibility between [TranslucentTB](https://github.com/TranslucentTB/TranslucentTB) and RoundedTB. This is experimental and *will* flicker slightly. It requires TranslucentTB version 2021.5 to function.
- **About RoundedTB** - provides information about the current version of RoundedTB. The "Debug" section lets you open the config and log files.

### Windows 11 23H2 and later

Windows 11 moved the taskbar app list into a XAML surface. RoundedTB now reads
the complete `TaskbarFrame` bounds, so dynamic/split layouts include all pinned
and running applications instead of only the first few buttons. Primary and
secondary taskbars are handled independently. If Explorer is restarted or
rebuilds a taskbar, RoundedTB releases the old automation objects, rediscovers
the handles and reapplies the current layout automatically.

### Long-running use

The background monitor retries transient Explorer/UI Automation failures,
restores the taskbar if an update fails, and uses bounded waits for optional
TranslucentTB refresh messages. Region handles created for dynamic layouts are
released after every update, so the process can remain in the tray for long
periods without accumulating GDI resources.







## Known issues
 - Auto-hiding is still incredibly experimental and may lead to a lot of flickering, especially with TranslucentTB compatibility or dynamic/split mode enabled. ([#36](https://github.com/torchgm/RoundedTB/issues/36))
 - Rounded corners are not antialiased due to a Windows limitation. ([#4](https://github.com/torchgm/RoundedTB/issues/4))
 - Dynamic mode won't hide the left side of the taskbar if the taskbar alignment has never been changed. This can be worked around by changing the alignment to Left and back to Center. ([#98](https://github.com/torchgm/RoundedTB/issues/98)) 
 - Dynamic mode/split mode only work correctly when the taskbar is horizontal at the top/bottom of the screen.
 - Split mode on Windows 10 only supports the main taskbar, secondary taskbars will not be split.
 - When using dynamic mode, the taskbar may occasionally become too large, too small or not update. This can usually be fixed by moving a window to or from that monitor or briefly changing the taskbar alignment. These issues will be reduced in upcoming updates, don't worry! I just need to refactor a lot of code first.
 - Compatibility with taskbar mods outside of TranslucentTB version 2021.5 is not currently guaranteed.

## Other info
RoundedTB makes no permanent changes (though it will run on startup if you
enable it from the tray icon). If Explorer is manually restarted, leave
RoundedTB running; it will wait for the new taskbar handles and restore the
layout. If anything breaks catastrophically, press
<kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Esc</kbd> to open Task Manager, end RoundedTB
and restart Explorer. At worst, reboot the PC.

Feel free to let me know about any bugs by filing an issue so I can look into it. Alternatively if you want to discuss RoundedTB, get some insider sneak-peeks, need some assistance or just want to see what I'm up to, then feel free to join the [Discord server](https://discord.gg/wYQJd8VGSB).

### Configuring split mode on Windows 10
Split mode has a couple of limitations and requires a small amount of setup to get working properly.
#### Limitations
- Split mode doesn't resize itself automatically. This feature will be coming to RoundedTB for Windows 10 in the future.
- Toolbars are not compatible with split mode currently, and will need to be disabled apart from one. This is because toolbars are used to mark the "empty" space on the taskbar.
- Split mode only works when the taskbar is horizontal at the top or bottom of the screen, and on the primary monitor.
#### Setup
1. Right-click the taskbar and disable "Lock the taskbar".
2. Right-click it again and turn off any existing toolbars.
3. Right-click a third time, select Toolbars > Desktop.
4. Use the small <kbd>||</kbd> handle to resize the taskbar as you please.

Watch the following video for a guide on setting up split mode:

https://user-images.githubusercontent.com/31840547/134795022-1312d011-40f2-4641-8c8d-3d6c0e752747.mp4

## Build and publish

The project targets `.NET 6 for Windows` and uses a COM reference for Windows
UI Automation. Use Visual Studio 2022 MSBuild (the `dotnet build` command does
not resolve this COM reference on all SDK versions):

```powershell
& "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" `
  RoundedTB.sln /t:Publish /p:Configuration=Release /p:Platform="Any CPU" `
  /p:RuntimeIdentifier=win-x64 /p:SelfContained=true /p:PublishSingleFile=true `
  /p:EnableCompressionInSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

The self-contained executable is written below
`RoundedTB\bin\Release\net6.0-windows10.0.19041\win-x64\publish`. Windows 10
and Windows 11 are supported; dynamic mode uses the Windows 11 taskbar
surface, while Windows 10 uses split mode and its existing toolbar setup.

