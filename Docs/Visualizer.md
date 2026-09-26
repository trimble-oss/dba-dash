# DBA Dash Visualizer

DBA Dash Visualizer opens SQL Server execution plans (`.sqlplan`) and deadlock graphs (`.xdl`) in the same graphical
viewers that are built into DBA Dash.  It is a small app of its own, for when you have a plan or a deadlock graph and no
DBA Dash: no repository, service, SQL Server connection or monitored instance is needed.

- **Execution plans** - the plan as a graph, with operator details, warnings, missing indexes, parameters, waits and insights.
- **Deadlock graphs** - the graph, the processes and resources involved, and findings.
- Several plans open on tabs of one window, so they can be compared; the same goes for deadlock graphs.

What needs DBA Dash - the AI analysis, collecting the current plan for a deadlocked statement from the instance, and the
Query Store lookup - isn't offered in the Visualizer.

## Requirements

- 64 bit Windows 10 or later.
- The [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (x64).  The winget package installs it
  for you.

## Install

**Zip.**  Download `DBADash_Visualizer_<version>.zip` from the
[latest release](https://github.com/trimble-oss/dba-dash/releases/latest) and extract it to a folder of your choice.  Run
`DBADashVisualizer.exe`.

**winget.**

```
winget install Trimble.DBADashVisualizer
```

The winget package is added to the winget repository some time after each release.

## Using it

- Run `DBADashVisualizer.exe` and choose a file, or pass the files on the command line:
  `DBADashVisualizer.exe MyPlan.sqlplan MyDeadlock.xdl`.  Dragging files onto a window opens them too.
- **Open from Explorer.**  From the Settings menu of either viewer, choose *Open .sqlplan Files with DBA Dash Visualizer*
  (or the `.xdl` equivalent) to offer it in Explorer's *Open with* menu.  Windows doesn't let an app make itself the
  default: choose *Make DBA Dash Visualizer the Default* to go to Settings > Default apps and pick it there.
  The same can be done from a command line - `DBADashVisualizer.exe --RegisterFileAssociation`, and
  `--UnregisterFileAssociation` to take it away.  This is per user and needs no elevation.
- Files opened from Explorer, several at once, all open in the one running copy.
- **Start menu.**  Neither the zip nor the winget package adds a Start menu entry.  Choose *Show in Start Menu* from the
  Settings menu of either viewer, or run `DBADashVisualizer.exe --CreateStartMenuShortcut`, to add one for your user; choose it
  again, or use `--RemoveStartMenuShortcut`, to take it away.  If you move the app to another folder, the shortcut is pointed at
  the new one the next time it runs.

The DBA Dash GUI has these viewers too, and registers its own entry for the same file types.  Both can be offered side by
side.

## Settings

Kept per user in `%LOCALAPPDATA%\DBADash\Visualizer\settings.json`.  Most are the choices made in the viewers' Settings
menus - the layout, edge widths and so on.  Two are edited by hand:

| Setting | Values | Default |
|---|---|---|
| `Theme` | `Default`, `White`, `Dark` | Follows the Windows light / dark app setting |
| `CheckForUpdates` | `true`, `false` | `true` (also set from *About*) |

The log is `%LOCALAPPDATA%\DBADash\Logs\DBADashVisualizer-<date>.log`.

## Updating

At most once a day, a few seconds after it starts, the Visualizer asks GitHub whether there is a newer release and says so
if there is.  It never downloads or installs anything by itself, and what it suggests depends on how you installed it:

- **From the zip:** choose *Download*, then extract the new zip over the old folder (close the Visualizer first).
- **With winget:** it tells you to run `winget upgrade Trimble.DBADashVisualizer` and offers to copy the command.  A new release
  can take a little while to reach winget, so if winget says there is nothing to upgrade, try again later.

*Skip This Version* stops it mentioning that version again.  Switch the check off in *About*, or look now with *Check for
Updates* in the Settings menu.

## Uninstall

If you registered it for files or added it to the Start menu, undo that first - run
`DBADashVisualizer.exe --UnregisterFileAssociation` and `--RemoveStartMenuShortcut` - as deleting the folder, or
`winget uninstall Trimble.DBADashVisualizer`, doesn't remove them.  Then delete the folder (or uninstall with winget).  Delete
`%LOCALAPPDATA%\DBADash\Visualizer` to remove its settings.

## For maintainers

The Visualizer is released with DBA Dash, at the same version.

1. **Build.**  The *Tag and Create Release* workflow runs `Scripts/Publish-Visualizer.ps1 -Unsigned`, which publishes the app,
   removes the debug symbols, checks that nothing that belongs to the rest of DBA Dash (the service, SMO, the AWS SDK...) came
   along, and creates `DBADash_Visualizer_<version>-unsigned.zip`.  That is added to the draft release with the other two
   zips.  CI runs the same script, without the zip, so a reference that pulls in more than it should fails the build.
2. **Sign.**  `Scripts/SignRelease.ps1` signs `DBADash*.exe` in each `-unsigned.zip` of the draft release - including
   `DBADashVisualizer.exe` - and uploads `DBADash_Visualizer_<version>.zip` alongside.  The in-app update check only offers a
   release with a zip that isn't `-unsigned`.
3. **Publish** the release.
4. **winget.**  Once the release is published, `Scripts/New-WingetManifest.ps1 -Version <version>` downloads the signed zip,
   works out its hash, writes the manifest under `DBADashBuild\winget` and validates it with `winget validate`.  Submit the
   result to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) as a pull request, or with
   [`wingetcreate submit`](https://github.com/microsoft/winget-create).  The script doesn't submit anything itself.

The code is in `DBADashVisualizer` (the app: start-up, updates, About) and `DBADash.Viewers.GUI` (the viewers, which the DBA
Dash GUI uses too).
