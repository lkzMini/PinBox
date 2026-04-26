# PinBox v0.1.1 Release Notes

PinBox v0.1.1 is a small polish release for the early MVP.

## Highlights

- Restores the previous window size on startup.
- Saves the window position when possible and clamps restored placement to the nearest display work area.
- Enforces a safer minimum window size so the main layout does not visually collapse when resized too small.
- Adds a reproducible win-x64 Release ZIP packaging script with clean-folder smoke test.

## Packaging

Public releases should use the ZIP artifact:

```text
artifacts/release/PinBox-v0.1.1-win-x64.zip
```

Create it locally with:

```powershell
.\scripts\package-win-x64.ps1
```

The script builds the Release win-x64 app, stages the WinUI bin output, excludes debug/dev-only files, creates the ZIP, extracts it to a clean smoke-test folder, and launches the extracted exe.

## Notes

- This is not an MSIX release.
- No self-signed certificate or certificate trust step is required.
- No admin install step is required by the packaging flow.
- PinBox remains an early MVP, not production-grade software.

## Manual QA checklist

1. Launch PinBox.
2. Resize the window to a normal working size and close it.
3. Relaunch PinBox and confirm the size is restored.
4. Move the window, close it, and relaunch. Confirm it restores on-screen or clamps to a visible display area.
5. Try resizing very small and confirm the app enforces a usable minimum size.
6. Create and edit a plain note; confirm typing does not lose focus.
7. Create a checklist note, click `+ Item`, type into the item, toggle it, and remove it; confirm no crash.
8. Restart and confirm saved notes still load.
9. Run `scripts/package-win-x64.ps1` and confirm the smoke test launches the extracted ZIP app successfully.
