# PinBox

PinBox is a small local-first WinUI 3 notes app for quick text notes and lightweight checklists.

## Status

This repository is an early MVP.

It is useful for local experimentation and basic note-taking, but it is not production-grade software yet.

## Current features

- Plain text notes
- Checklist notes
- Pin / unpin notes
- Archive / unarchive notes
- Search by title, note content, and checklist item text
- Local JSON persistence
- Basic color variants for note cards

## Known limitations

- Windows-only desktop app
- Local storage only; no sync, accounts, or cloud backup
- No formal automated test coverage yet
- Editing stability has been improved, but this is still an MVP and should be treated as such

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

## Notes for contributors

- Keep changes small and focused
- Prefer stability fixes over feature work
- Avoid broad UI refreshes during active editing flows
