# WUWatch (winsanity)

A Windows-only .NET 10 WinForms tray application that warns the user when
Windows Update is causing sustained heavy system load.

Windows Update can perform heavy background servicing with very little visible
indication. Disk active time can reach 100%, CPU can spike, and the machine can
become nearly unusable. WUWatch lives in the system tray, subscribes to the
Windows Update event log, and — only once relevant update activity is seen —
starts sampling disk active time and CPU. When sustained load crosses
configured thresholds it shows a large, TopMost warning window.

## Current state

- .NET 10, WinForms, C#, x64, Windows 11.
- Tray application using `ApplicationContext`; no separate window in normal use.
- Event-driven (`EventLogWatcher`) on
  `Microsoft-Windows-WindowsUpdateClient/Operational`. Event ID 41 arms
  monitoring; additional relevant IDs can be added via settings.
- State machine: `IDLE -> ARMED -> WARNING -> ARMED -> IDLE`.
  - ARMED: samples disk active time and CPU once per second, keeps 30-second
    rolling averages, stays armed 8 hours after the last relevant event.
  - WARNING: shown when rolling disk avg >= 80% or CPU avg >= 85%. Hidden only
    after both metrics stay below 50%/60% continuously for 15 minutes.
- Warning window shows live + averaged disk/CPU, episode timestamps, and a
  quiet-time countdown. Dismissing requires deliberate confirmation; the app is
  never hostile to termination.
- Settings in `%LOCALAPPDATA%\WUWatch\appsettings.json` (JSON, fallback to
  defaults on missing/malformed file).
- Diagnostic log at `%LOCALAPPDATA%\WUWatch\wuwatch.log`.
- Debug simulation controls (tray menu) to exercise the state machine without
  waiting for a real Windows Update.
- Unit tests cover rolling averages, threshold/hysteresis logic, and state
  transitions (pure logic separated from Windows APIs).

## Prerequisites

- .NET 10 SDK (Windows desktop workload).
- For the installer: Inno Setup 6
  (`choco install innosetup`), automatically installed by `geninstaller.ps1`
  if missing.

## Build & test

```
.\winbuild.ps1              # dotnet build (Debug/Release)
dotnet test                 # unit tests
```

## Installer

```
.\geninstaller.ps1          # publish + compile with Inno Setup
```

What it does:

1. Publishes a self-contained, single-file win-x64 build to
   `installer\publish\` (`WUWatch.exe`, ~116 MB).
2. Compiles it with Inno Setup into a per-user setup in `installer\output\`,
   date-stamped, e.g. `WUWatch_260910_Installer.exe`.

Installer details (`installer\wuinno.iss`):

- Per-user install dir `%LOCALAPPDATA%\Programs\WUWatch`, no admin (UAC)
  required (`PrivilegesRequired=lowest`).
- x64 only (`ArchitecturesInstallIn64BitMode=x64compatible`).
- Start Menu shortcuts `WUWatch\WUWatch` and uninstaller.
- Uninstall does not touch app data/logs in `%LOCALAPPDATA%\WUWatch`.

## Layout

```
WUWatch/            app (Program.cs, TrayApplicationContext, WindowsUpdateWatcher,
                    SystemLoadMonitor, RollingAverage, UpdateEpisode,
                    WarningForm, AppSettings, AppLogger, WUWatchEngine)
WUWatch.Tests/      unit tests
installer/          wuinno.iss, publish/ (build output), output/ (setup exe)
geninstaller.ps1    publish + Inno Setup compile
winbuild.ps1        dotnet build wrapper
tasks/              working notes
```