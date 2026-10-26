# MacTopBar

MacTopBar is a Windows 11 desktop application written in C# and WPF. It creates a macOS-inspired top menu bar on every detected monitor and uses native Windows calls for foreground-app tracking and power/network state. It does not use a browser, Rainmeter, Steam, Seelen UI, Windhawk, or paid software.

## Features

- Full-width 34-pixel bar on each detected display, with an Apple-inspired mark and File / Edit / View / Window / Help menus.
- Dark and light themes, adjustable transparency, attempted Windows acrylic blur, and saved preferences (`%APPDATA%\MacTopBar\settings.json`).
- Tracks the foreground Windows process and shows its friendly application name.
- Actual Wi-Fi interface state through the Windows WLAN API, general network availability through Windows network-interface APIs, and battery / AC / charging data through `GetSystemPowerStatus`.
- Live local date and clock.
- Menu commands for Windows Settings, File Explorer, Task Manager, Lock PC, closing/minimizing/maximizing the last foreground app, common editing shortcuts, refresh/zoom/full-screen shortcuts, app switching, and exit.
- Optional sign-in launch stored for the current user under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- Uses the Windows AppBar API to request space at the top rather than intentionally covering the normal work area. Windows may handle third-party appbar reservations differently across multi-monitor configurations.
- The Bluetooth indicator is not shown in this dependency-free build because the native APIs used here do not provide a definitive Bluetooth radio on/off state. The bar does not display a fake Bluetooth state.

## Get a ready-to-run EXE without installing the SDK on your PC

This project includes a GitHub Actions workflow at `.github/workflows/build-windows.yml`. GitHub runs the Windows build on its cloud runner and publishes downloadable artifacts; you do **not** need to install .NET on your own computer.

1. Create a repository on GitHub.
2. Upload the contents of this project folder, including the hidden `.github/workflows/build-windows.yml` file, and commit the files to the `main` branch. Keep `MacTopBar.csproj` in the repository root.
3. Open the repository's **Actions** tab and select **Build MacTopBar for Windows**. If the first build did not start automatically, press **Run workflow**.
4. Open the completed run and download either `MacTopBar-win-x64-EXE` or `MacTopBar-win-x64-ZIP` from **Artifacts**.
5. Extract the ZIP if you downloaded it, then run `MacTopBar.exe` on a 64-bit Windows 11 PC.

The cloud build uses .NET 10 only on GitHub's temporary runner. The resulting application is self-contained and does not require the .NET SDK or runtime on your computer. The workflow has been added to the project source, but it still needs to be run on GitHub before a compiled EXE can be downloaded. Only download and run builds from a repository you trust.

## Build on Windows

1. Install the free **.NET 10 SDK** from Microsoft's .NET downloads page: <https://dotnet.microsoft.com/en-us/download/dotnet/10.0>.
2. Open PowerShell in this folder.
3. Run:

```powershell
.\build.ps1
```

Or run `dotnet build .\MacTopBar.csproj -c Release` directly. The project uses WPF and native Windows monitor enumeration APIs. There are no third-party NuGet package references.

## Publish a standalone EXE

For a standard 64-bit Windows 11 computer, run:

```powershell
.\publish.ps1
```

The single-file, self-contained output is written to `publish\win-x64\MacTopBar.exe`. To publish for an ARM64 Windows device, run:

```powershell
.\publish.ps1 -RuntimeIdentifier win-arm64
```

The executable is self-contained, so the target computer does not need the .NET runtime separately. The build computer still needs the SDK to compile/publish the project.

## Install / uninstall

After publishing, run:

```powershell
.\install.ps1
```

The installation script copies the executable to `%LOCALAPPDATA%\Programs\MacTopBar` and creates a Start Menu shortcut. It does not require administrator rights. To remove the application, run:

```powershell
.\uninstall.ps1
```

Your saved preferences in `%APPDATA%\MacTopBar` are retained after uninstall.

## Using MacTopBar

- Click the Apple-inspired mark for MacTopBar Settings, Windows Settings, File Explorer, Task Manager, Lock PC, and Quit.
- Click File, Edit, View, Window, or Help to open the corresponding dropdown.
- In Settings, choose Dark / Light, set transparency, and enable or disable automatic startup.
- Choose **Quit MacTopBar** in the Apple menu to exit the app.

## Notes and limitations

- Edit and View actions send conventional keyboard shortcuts to the last foreground application. An individual application may not implement every shortcut.
- The application name is derived from the owning process; uncommon process names are displayed in title case.
- Acrylic blur uses an undocumented Windows composition entry point. If Windows blocks or changes that feature, the bar falls back to its translucent solid-color background.
- Native system calls and AppBar behavior need runtime-testing on Windows 11 hardware. The authoring environment for this package had no .NET SDK and couldn't reach the internet, so an actual compile could not be run here. Run `build.ps1` on Windows to verify compilation locally before relying on the executable.
