# PinBox

PinBox is a small local-first WinUI 3 notes app for quick text notes and lightweight checklists.

## Status

PinBox is an early MVP. v0.1.1 focuses on polish and release reliability, not new features.

It is useful for local experimentation and basic note-taking, but it is not production-grade software.

## Current features

- Plain text notes
- Checklist notes
- Pin / unpin notes
- Archive / unarchive notes
- Search by title, note content, and checklist item text
- Local JSON persistence
- Basic color variants for note cards
- Window size restore across restarts

## Known limitations

- Windows-only desktop app
- Local storage only; no sync, accounts, or cloud backup
- No formal automated test coverage yet
- Window position restore is clamped to the nearest detected display area and may adjust after monitor changes

## Run locally

### Requirements

- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 or later with WinUI / Windows App SDK support recommended

### From Visual Studio

1. Open `D:\projects\PinBox\PinBox.sln`
2. Set `PinBox.App` as the startup project if needed
3. Run the app

### From the command line

```powershell
dotnet build D:\projects\PinBox\PinBox.sln
dotnet run --project D:\projects\PinBox\src\PinBox.App\PinBox.App.csproj
```

## Release ZIP

The public release artifact is a ZIP, not an MSIX.

To create and smoke-test a local win-x64 ZIP:

```powershell
.\scripts\package-win-x64.ps1
```

Expected output:

```text
artifacts\release\PinBox-v0.1.1-win-x64.zip
```

## Notes for contributors

- Keep changes small and focused
- Prefer stability fixes over feature work
- Avoid broad UI refreshes during active editing flows
- Do not manually edit generated `bin/`, `obj/`, or `artifacts/` output
