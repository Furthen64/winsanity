[Setup]
AppId={{72A3EBB0-2486-4166-896D-D2811C86D6E0}
AppName=WUWatch
AppVersion=1.0.0
AppPublisher=WUWatch
AppComments=Windows Update load monitor
DefaultDirName={localappdata}\Programs\WUWatch
DefaultGroupName=WUWatch
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#SourcePath}output
OutputBaseFilename=WUWatch_{#GetDateTimeString('yymmdd','','')}_Installer
SetupLogging=yes
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
UninstallDisplayName=WUWatch
UninstallDisplayIcon={app}\WUWatch.exe

[Files]
Source: "{#SourcePath}publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\WUWatch\WUWatch"; Filename: "{app}\WUWatch.exe"; WorkingDir: "{app}"
Name: "{userprograms}\WUWatch\Uninstall WUWatch"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\WUWatch.exe"; Description: "Launch WUWatch"; Flags: nowait postinstall skipifsilent unchecked