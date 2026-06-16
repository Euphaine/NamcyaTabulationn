[Setup]
AppName=NAMCYA Tabulation System
AppVersion=2.0
AppPublisher=NAMCYA
DefaultDirName={autopf}\NAMCYA Tabulation System
DefaultGroupName=NAMCYA Tabulation System
OutputDir=InstallerOutput
OutputBaseFilename=NAMCYA_Tabulation_Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile=namcya_logo.ico


[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\NAMCYA Tabulation System"; Filename: "{app}\NamcyaTabulation.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\NAMCYA Tabulation System"; Filename: "{app}\NamcyaTabulation.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\NamcyaTabulation.exe"; WorkingDir: "{app}"; Description: "{cm:LaunchProgram,NAMCYA Tabulation System}"; Flags: postinstall skipifsilent