# Instant Transparent Builds — sg0hann

A Windows WPF desktop companion for **CD Revive** with visual previews of transparent building levels, local backup/restore for user-selected files, game folder shortcuts, and GitHub release update checks.

**This app is a visual preview and file-management companion. It does not patch CD Revive, alter game effects, inject code, or bypass anti-cheat.** The level presets only affect the on-screen preview.

## Windows build

Requires .NET 8 SDK on Windows.

```powershell
dotnet publish src/InstantTransparentBuilds/InstantTransparentBuilds.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

The executable is under `src/InstantTransparentBuilds/bin/Release/net8.0-windows/win-x64/publish/`.

GitHub Actions `Windows Build` publishes a downloadable exe artifact for pushes and a downloadable exe on tagged releases (e.g. `v0.1.0`). Open **Actions → Windows Build → Artifacts** after the workflow completes.

## Features

- Minimal dark desktop dashboard
- Live colored structure previews for Lv.1 gray, Lv.2 cyan, Lv.3 blue and Lv.4 purple
- Adjustable preview transparency and animations disabled in preview
- Choose game directory (only opens the folder, no modifications)
- Back up any user-selected file to a private app data folder and restore to a user-selected location
- Check GitHub Releases for newer published versions; confirms before opening the official release download page
- No admin access, process hooks, Defender exclusions, stealth features or game file edits

## Defender / signing

No program can guarantee no antivirus flags. To build trust: publish transparent source, sign binaries with a valid Authenticode certificate, avoid obfuscation and bundled unknown software, and submit false positives to Microsoft for review. Do not add Defender exclusions.

## Security

Backup file selection is manual; restore requires an explicit path and confirmation. For restoration, close any program using the destination file. Always comply with game rules.
