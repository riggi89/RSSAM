; RSSAM installer definition.
; Copyright (c) 2026 Daniel Riggi (riggi89).
; Distributed under the project license; see LICENSE.md and NOTICE.md.

#define MyAppVersion GetEnv("RSSAM_VERSION")
#define SourceRoot GetEnv("RSSAM_SOURCE_ROOT")
#define InstallerLicense GetEnv("RSSAM_INSTALLER_LICENSE")
#define MyAppArchitecture GetEnv("RSSAM_ARCHITECTURE")

#if MyAppArchitecture == "x64"
    #define RuntimeIdentifier "win-x64"
    #define AllowedArchitectures "x64compatible"
#elif MyAppArchitecture == "x86"
    #define RuntimeIdentifier "win-x86"
    #define AllowedArchitectures "x86compatible"
#else
    #error Unsupported or missing RSSAM_ARCHITECTURE value
#endif

[Setup]
AppId={{F99429D7-C0F7-43A8-9368-407534934825}
AppName=RSSAM
AppVersion={#MyAppVersion}
AppVerName=RSSAM {#MyAppVersion}
AppPublisher=Daniel Riggi (riggi89)
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany=Daniel Riggi (riggi89)
VersionInfoDescription=RSSAM {#MyAppArchitecture} Installer
VersionInfoProductName=RSSAM - Riggi's Steam Achievement Manager

SetupIconFile={#SourceRoot}\src\RSSAM.App\Assets\RSSAM-AppIcon.ico
UninstallDisplayIcon={app}\RSSAM.exe

DefaultDirName={localappdata}\Programs\RSSAM
DefaultGroupName=RSSAM
DisableProgramGroupPage=yes
PrivilegesRequired=lowest

ArchitecturesAllowed={#AllowedArchitectures}

#if MyAppArchitecture == "x64"
ArchitecturesInstallIn64BitMode=x64compatible
#endif

MinVersion=10.0.17763
LicenseFile={#InstallerLicense}

OutputDir={#SourceRoot}\artifacts\installer
OutputBaseFilename=RSSAM_{#MyAppVersion}-{#RuntimeIdentifier}-Setup

Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern dynamic

CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; \
    Description: "{cm:CreateDesktopIcon}"; \
    GroupDescription: "{cm:AdditionalIcons}"; \
    Flags: unchecked

[Files]
Source: "{#SourceRoot}\artifacts\publish\{#RuntimeIdentifier}\*"; \
    DestDir: "{app}"; \
    Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\RSSAM"; \
    Filename: "{app}\RSSAM.exe"; \
    WorkingDir: "{app}"

Name: "{autodesktop}\RSSAM"; \
    Filename: "{app}\RSSAM.exe"; \
    WorkingDir: "{app}"; \
    Tasks: desktopicon

[Run]
Filename: "{app}\RSSAM.exe"; \
    WorkingDir: "{app}"; \
    Description: "{cm:LaunchProgram,RSSAM}"; \
    Flags: nowait postinstall skipifsilent