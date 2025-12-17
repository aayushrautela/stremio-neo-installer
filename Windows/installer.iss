; Updates Stremio shortcuts to launch Neo Web UI.

#define MyAppName "Stremio Neo Patcher"
#define MyAppVersion "1.0"
#define MyAppPublisher "Aayush Codes"

; URL must be provided via command line argument /DMyAppURL="..."
#ifndef MyAppURL
  #error "MyAppURL is undefined. Pass it via command line."
#endif

[Setup]
AppId={{A1B2C3D4-E5F6-7890-1234-56789ABCDEF0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
; Output directory
OutputDir=build
OutputBaseFilename=StremioNeo-Patcher
Compression=lzma
SolidCompression=yes
; No uninstall entry required
CreateUninstallRegKey=no
UpdateUninstallLogAppName=no
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Code]
var
  TargetURL: String;

// Updates a single shortcut if it exists
procedure PatchShortcut(ShortcutPath: String);
var
  WShell, Shortcut: Variant;
  Args: String;
begin
  if FileExists(ShortcutPath) then
  begin
    try
      WShell := CreateOleObject('WScript.Shell');
      Shortcut := WShell.CreateShortcut(ShortcutPath);
      Args := Shortcut.Arguments;

      // Append URL if missing
      if Pos(TargetURL, Args) = 0 then
      begin
        if Length(Args) = 0 then
          Shortcut.Arguments := '--url="' + TargetURL + '"'
        else
          Shortcut.Arguments := Args + ' --url="' + TargetURL + '"';
        
        Shortcut.Save;
      end;
    except
      // Ignore errors silently
    end;
  end;
end;

// Main execution logic
procedure CurStepChanged(CurStep: TSetupStep);
var
  DesktopPath, StartMenuPath, LnvPath: String;
begin
  if CurStep = ssPostInstall then
  begin
    TargetURL := '{#MyAppURL}';
    
    // Define shortcut paths
    DesktopPath := ExpandConstant('{userdesktop}\Stremio.lnk');
    StartMenuPath := ExpandConstant('{userprograms}\Stremio.lnk');
    LnvPath := ExpandConstant('{userappdata}\Microsoft\Windows\Start Menu\Programs\LNV\Stremio\Stremio.lnk');

    // Execute patch
    PatchShortcut(DesktopPath);
    PatchShortcut(StartMenuPath);
    PatchShortcut(LnvPath);
    
    MsgBox('Success! Stremio shortcuts updated to: ' + TargetURL, mbInformation, MB_OK);
  end;
end;