[Setup]
AppId={{B1E3A4D7-5F2C-4A8B-9D1E-6C7F3A2B5E8D}
AppName=Asistente
AppVersion=1.0
AppPublisher=Asistente
DefaultDirName={autopf}\Asistente
DefaultGroupName=Asistente
OutputDir=C:\Users\User\Desktop\Asistente\Installer
OutputBaseFilename=Asistente-Setup-1.0
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
SetupIconFile=C:\Users\User\Desktop\Asistente\Asistente\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\appicon.ico
UninstallDisplayIcon={app}\Asistente.exe
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0.17763

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"; GroupDescription: "Iconos adicionales:"; Flags: checkedonce

[Files]
Source: "C:\Users\User\Desktop\Asistente\Asistente\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Asistente"; Filename: "{app}\Asistente.exe"
Name: "{group}\Desinstalar Asistente"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Asistente"; Filename: "{app}\Asistente.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Asistente.exe"; Description: "Ejecutar Asistente ahora"; Flags: nowait postinstall skipifsilent
