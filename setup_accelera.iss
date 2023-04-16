; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Accelera"
#define MyAppVersion "1.0.0.0"
#define MyAppPublisher "Munich Institute of Biomedical Engineering"
#define MyAppURL "http://www.bioengineering.tum.de/"
#define MyAppExeName "Accelera.exe"

#define UseDotNet48

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)

;SignTool=signtool
AppId={{0B4AC5D8-F494-4250-A338-BB57E3EB57A3}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\MIBE/Accelera
DefaultGroupName=Munich Institute of Biomedical Engineering/Accelera
AllowNoIcons=yes
;InfoAfterFile=D:\Arbeit\Projekte\Atomic\BMS\InnoSetup\changelog.txt
;OutputDir=\InnoSetup\setup
OutputBaseFilename=setup_accelera
;Password=&ematric
;Encryption=yes
Compression=lzma
SolidCompression=yes
WizardImageFile=InnoSetup\Screen.bmp
WizardSmallImageFile=InnoSetup\logo.bmp
CreateUninstallRegKey=yes

DisableReadyPage=no
DisableReadyMemo=no

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"; LicenseFile: "InnoSetup\LICENCE.rtf"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"; LicenseFile: "InnoSetup\LICENCE.rtf"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 0,6.1

[Files]
Source: "Accelera\Accelera\bin\Release\*"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "Accelera\Accelera\bin\Release\app.publish\*"; DestDir: "{app}\bin\app.publish"; Flags: ignoreversion
;Source: "Accelera\Accelera\bin\Release\x64\*"; DestDir: "{app}\bin\x64"; Flags: ignoreversion
;Source: "Accelera\Accelera\bin\Release\x86\*"; DestDir: "{app}\bin\x86"; Flags: ignoreversion
;Source: "ARTEMIS\bin\Debug\Renci.SshNet.dll"; DestDir: "{app}\bin\x64"; Flags: ignoreversion
;Source: "ARTEMIS\bin\Debug\Renci.SshNet.xml"; DestDir: "{app}\bin\x86"; Flags: ignoreversion

Source: "InnoSetup\ChangeLog.txt"; DestDir: "{app}\Daten"; Flags: ignoreversion


;Source: "InnoSetup\src\*"; DestDir: "{userdocs}\Munich Institute of Biomedical Engineering\Accelera\Data"; Flags: ignoreversion  uninsneveruninstall onlyifdoesntexist  


[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\bin\{#MyAppExeName}"
; Name: "{group}\{cm:ProgramOnTheWeb,{#MyAppName}}"; Filename: "{#MyAppURL}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\bin\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\bin\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\bin\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// shared code for installing the products
#include "InnoSetup\scripts\dotnetfx.iss"
#include "InnoSetup\scripts\products.iss"
// helper functions
#include "InnoSetup\scripts\products\stringversion.iss"
#include "InnoSetup\scripts\products\winversion.iss"
#include "InnoSetup\scripts\products\fileversion.iss"
#include "InnoSetup\scripts\products\dotnetfxversion.iss"
// actual products



function InitializeSetup(): boolean;
begin
	// initialize windows version
	initwinversion();

  Dependency_AddDotNet48; // min allowed version is 4.5.2

	Result := true;
end;

