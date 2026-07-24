; Inno Setup script for XephyreFX.
; Run from the repo root: ISCC.exe packaging\windows\xephyrefx.iss
; Expects a self-contained publish already built at publish\win-x64
; (dotnet publish Apps\XephyreFX.App\XephyreFX.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish\win-x64)

[Setup]
AppId={{6C6B7A0E-6E3B-4D3E-9C0A-8A2F1B2D9C11}
AppName=XephyreFX
AppVersion=0.1.0
AppPublisher=XephyreFX contributors
DefaultDirName={autopf}\XephyreFX
DefaultGroupName=XephyreFX
DisableProgramGroupPage=yes
OutputBaseFilename=XephyreFX-Setup
OutputDir=..\..\..\dist
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible

[Files]
Source: "..\..\..\publish\win-x64\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\XephyreFX"; Filename: "{app}\XephyreFX.exe"
Name: "{autodesktop}\XephyreFX"; Filename: "{app}\XephyreFX.exe"
Name: "{userstartup}\XephyreFX"; Filename: "{app}\XephyreFX.exe"; Tasks: startup

[Tasks]
Name: "startup"; Description: "Run XephyreFX automatically when Windows starts"; Flags: unchecked

[Run]
Filename: "{app}\XephyreFX.exe"; Description: "Launch XephyreFX"; Flags: nowait postinstall skipifsilent
