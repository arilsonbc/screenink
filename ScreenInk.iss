#define AppName "ScreenInk"
#define AppVersion "0.2.0"
[Setup]
#ifdef TestBuild
AppId=ScreenInk-Validation-020
#else
AppId={{F15AB71E-0375-4F81-90B3-26D825D2EE8F}
#endif
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=arilsonbc
AppPublisherURL=https://github.com/arilsonbc/screenink
AppSupportURL=https://github.com/arilsonbc/screenink/issues
DefaultDirName={localappdata}\Programs\ScreenInk
DefaultGroupName=ScreenInk
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
MinVersion=10.0
#ifdef TestBuild
OutputDir=..\..\work
OutputBaseFilename=ScreenInk-TestSetup-0.2.0
#else
OutputDir=.
OutputBaseFilename=ScreenInk-Setup-0.2.0
#endif
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=branding\setup.ico
WizardImageFile=branding\wizard.bmp
WizardSmallImageFile=branding\wizard-small.bmp
UninstallDisplayName=ScreenInk
UninstallDisplayIcon={app}\ScreenInk.exe
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos:"
[Files]
Source: "app\ScreenInk-0.2.0-final.exe"; DestName: "ScreenInk.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "COMO-USAR.txt"; DestDir: "{app}"; Flags: ignoreversion
[Icons]
Name: "{group}\ScreenInk"; Filename: "{app}\ScreenInk.exe"
Name: "{autodesktop}\ScreenInk"; Filename: "{app}\ScreenInk.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\ScreenInk.exe"; Description: "Abrir ScreenInk"; Flags: nowait postinstall skipifsilent
[Code]
function InitializeSetup(): Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 461808);
  if not Result then
    MsgBox('O ScreenInk precisa do .NET Framework 4.7.2 ou mais recente. Atualize o Windows ou instale esse componente pela Microsoft antes de continuar.', mbError, MB_OK);
end;




