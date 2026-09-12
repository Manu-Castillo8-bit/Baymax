[Setup]
AppId={{B1E3A4D7-5F2C-4A8B-9D1E-6C7F3A2B5E8D}
AppName=Asistente
AppVersion=1.1
AppPublisher=Asistente
DefaultDirName={autopf}\Asistente
DefaultGroupName=Asistente
OutputDir=C:\Users\User\Desktop\Asistente\Installer
OutputBaseFilename=Asistente-Setup-1.1
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

[Registry]
; Registro del AUMID + CLSID de activación (notificaciones de apps no empaquetadas)
; para que Windows muestre los recordatorios programados aunque la app esté cerrada.
Root: HKCU; Subkey: "Software\Classes\AppUserModelId\AsistenteApp"; ValueType: string; ValueName: "DisplayName"; ValueData: "Asistente"; Flags: uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\AppUserModelId\AsistenteApp"; ValueType: string; ValueName: "IconUri"; ValueData: "{app}\Asistente.exe,0"; Flags: uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Classes\AppUserModelId\AsistenteApp"; ValueType: string; ValueName: "CustomActivator"; ValueData: "{{B1E3A4D7-5F2C-4A8B-9D1E-6C7F3A2B5E81}"; Flags: uninsdeletekeyifempty
; CLSID del activador: al pulsar la notificación lanza la aplicación
Root: HKCU; Subkey: "Software\Classes\CLSID\{{B1E3A4D7-5F2C-4A8B-9D1E-6C7F3A2B5E81}\LocalServer32"; ValueType: string; ValueName: ""; ValueData: "{app}\Asistente.exe"; Flags: uninsdeletekeyifempty

[Icons]
Name: "{group}\Asistente"; Filename: "{app}\Asistente.exe"
Name: "{group}\Desinstalar Asistente"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Asistente"; Filename: "{app}\Asistente.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Asistente.exe"; Description: "Ejecutar Asistente ahora"; Flags: nowait postinstall skipifsilent
