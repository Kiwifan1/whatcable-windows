; Inno Setup script for the unpackaged (non-Store) WhatCable distribution.
;
; The WinUI 3 app ships as WindowsPackageType=None, so it can be xcopy-deployed
; rather than installed as MSIX. Publish a self-contained build first, then point
; this script at the publish output:
;
;   dotnet publish src\WhatCable.Windows.App\WhatCable.Windows.App.csproj ^
;     -c Release -r win-x64 --self-contained ^
;     -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true ^
;     -o publish\win-x64
;
;   iscc installer\WhatCable.iss /DSourceDir=publish\win-x64
;
; Launch-at-login for this build is handled entirely by the app's own setting, via
; StartupTaskService's per-user "Run" registry fallback (the packaged StartupTask only
; exists for the MSIX build). The installer intentionally does not write any Run key or
; Startup-folder shortcut, so the in-app toggle stays the single source of truth.

#ifndef SourceDir
  #define SourceDir "..\publish\win-x64"
#endif

#ifndef AppVersion
  #define AppVersion "0.0.1"
#endif

#define AppName "WhatCable"
#define AppPublisher "WhatCable contributors"
#define AppExeName "WhatCable.exe"
#define AppUrl "https://whatcable.uk"

[Setup]
AppId={{6F3B6F2C-2C3E-4C8E-9C5B-9D9D2F1A8E10}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
OutputBaseFilename=WhatCable-Setup-{#AppVersion}
UninstallDisplayIcon={app}\{#AppExeName}
; v1 ships unsigned; codesigning is added once a certificate is available.

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent
