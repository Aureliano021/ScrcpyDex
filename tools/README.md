# Build Tools

This folder contains build-time scripts and developer utilities.

## BuildIcon (`BuildIcon.cs`)

Utility to regenerate multi-resolution application icons (`.ico`) containing standard layers (16x16, 32x32, 48x48, 64x64, 128x128, 256x256).

### Compilation
To compile `BuildIcon.cs` using the standard .NET Framework / Roslyn C# compiler (`csc.exe`):

```cmd
csc /target:exe /out:BuildIcon.exe /r:System.Drawing.dll BuildIcon.cs
```

### Execution
Run the compiled utility providing the source high-resolution artwork:

```cmd
BuildIcon.exe <path-to-source-image.png>
```

The output `app.ico` will be written to the local execution folder.

---

## dotnet-install.ps1

Official Microsoft .NET installation script used by CI pipelines to bootstrap the .NET 8 SDK on Windows build runners.
