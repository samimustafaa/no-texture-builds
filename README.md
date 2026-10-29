# Instant Transparent Builds — sg0hann

A Windows WPF desktop companion for **CD Revive** with visual previews of transparent building levels, local backup/restore for user-selected files, game folder shortcuts, and GitHub release updates with automatic download, installation, and application relaunch.

**This app is a visual preview and file-management companion. It does not patch CD Revive, alter game effects, inject code, or bypass anti-cheat.** The level presets only affect the on-screen preview.

## Windows build

Requires .NET 8 SDK on Windows.

```powershell
dotnet publish src/InstantTransparentBuilds/InstantTransparentBuilds.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

The executable is under `src/InstantTransparentBuilds/bin/Release/net8.0-windows/win-x64/publish/`.

GitHub Actions `Windows Build` publishes a downloadable exe artifact for pushes and a checksummed release ZIP on tagged releases (e.g. `v0.2.0`). Open **Actions → Windows Build → Artifacts** after the workflow completes.

## Features

- Minimal dark desktop dashboard
- Live colored structure previews for Lv.1 gray, Lv.2 cyan, Lv.3 blue and Lv.4 purple
- Adjustable preview transparency and animations disabled in preview
- Choose game directory (only opens the folder, no modifications)
- Back up any user-selected file to a private app data folder and restore to a user-selected location
- Check GitHub Releases for newer published versions; downloads the latest GitHub release, verifies SHA-256, installs after confirmation and restarts
- No admin access, process hooks, Defender exclusions, stealth features or game file edits

## Defender / signing

No program can guarantee no antivirus flags. To build trust: publish transparent source, sign binaries with a valid Authenticode certificate, avoid obfuscation and bundled unknown software, and submit false positives to Microsoft for review. Do not add Defender exclusions.

## Security

Backup file selection is manual; restore requires an explicit path and confirmation. For restoration, close any program using the destination file. Always comply with game rules.

## Auto-installing updates

Each release must contain `InstantTransparentBuilds-win-x64.zip` and `InstantTransparentBuilds-win-x64.zip.sha256`. The updater verifies the archive checksum, stages both executables, closes the running app, copies the replacement executable, and restarts it. Requires write permissions in the install folder; put the app in a user-owned directory rather than Program Files. The release ZIP contains two executables; keep both together for first install. If an update fails, see `%TEMP%\\InstantTransparentBuilds-update-error.txt`.

The app currently checks for updates when you click the button. Installing an available update is automatic after your confirmation. Release ZIPs are not Authenticode signed by default; code signing remains recommended.
