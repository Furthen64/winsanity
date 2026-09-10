# WUWatch Installer

State as of 2026-09-10: Inno Setup 6.7.1 installed via choco. Publish of
self-contained single-file win-x64 build completed successfully
(installer\publish\WUWatch.exe, ~116 MB). `.\geninstaller.ps1` (was
winsetup.ps1) now builds and compiles end-to-end.

Done:
- installer\wuinno.iss: Source uses `{#SourcePath}publish\*`.
- installer\wuinno.iss: OutputDir uses `{#SourcePath}output` (= installer\output),
  matching geninstaller.ps1's expected path (was `..\output`, which wrote to the
  repo-root output\ folder).
- installer\wuinno.iss: OutputBaseFilename is date-stamped, e.g.
  `WUWatch_260910_Installer.exe`, via
  `{#GetDateTimeString('yymmdd','','')}`.
- geninstaller.ps1: prefers the real `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`
  over the chocolatey shim; reports the newest built setup exe.
- Final installer: `installer\output\WUWatch_260910_Installer.exe` (setup stub
  is x86, as normal for Inno; app installs x64-only via
  ArchitecturesInstallIn64BitMode).

Remaining (manual smoke test on this machine):
1. Run `.\installer\output\WUWatch_260910_Installer.exe`.
2. Confirm no UAC prompt (PrivilegesRequired=lowest).
3. Confirm Start Menu shortcut `WUWatch\WUWatch` launches the app.
4. Confirm uninstaller removes shortcuts and app files.
5. Confirm app data/logs stay in %LOCALAPPDATA%\WUWatch (installer must not
   delete them on uninstall - currently it does not).
6. Update AppVersion in wuinno.iss when bumping the app version.
7. Optional future: add per-user startup (HKCU\...\Run) behind a setting -
   structure in the tray app is not yet wired for it.