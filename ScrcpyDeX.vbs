' ScrcpyDeX — Zero-Flash Desktop Launcher
' Runs ScrcpyDeX WinUI Control Center without any console window flashing
Set WshShell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
scriptDir = fso.GetParentFolderName(WScript.ScriptFullName)
ps1File = scriptDir & "\client\ScrcpyDeX-WinUI.ps1"

On Error Resume Next
WshShell.Run "pwsh.exe -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File """ & ps1File & """", 0, False
If Err.Number <> 0 Then
    WshShell.Run "powershell.exe -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File """ & ps1File & """", 0, False
End If
