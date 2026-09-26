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

**Setup program.**  Download `DBA_Dash_Visualizer_Setup_<version>.exe` from the
[latest release](https://github.com/trimble-oss/dba-dash/releases/latest) and run it.  It installs in a few seconds, for your
user only, so it needs no administrator rights: no questions, and no windows to click through.  It adds DBA Dash Visualizer to
your Start menu and to *Installed apps*, and installs the .NET Desktop Runtime if you don't have it.  This is the one that
updates itself.

**Zip.**  Download `DBADash_Visualizer_<version>.zip` from the same page and extract it to a folder of your choice.  Run
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
- **Start menu.**  The setup program adds a Start menu entry.  The zip and the winget package don't.  Choose *Show in Start Menu* from the
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
if there is.  What it does about it depends on how you installed it:

- **From the setup program:** choose *Install Update*.  It downloads the update, showing progress, and installs it when you close
  the Visualizer - your windows aren't closed to do it.  Only what changed is downloaded when it can be.
- **From the zip:** choose *Download*, then extract the new zip over the old folder (close the Visualizer first).
- **With winget:** it tells you to run `winget upgrade Trimble.DBADashVisualizer` and offers to copy the command.  A new release
  can take a little while to reach winget, so if winget says there is nothing to upgrade, try again later.

*Skip This Version* stops it mentioning that version again.  Switch the check off in *About*, or look now with *Check for
Updates* in the Settings menu.

## Uninstall

**Setup program:** uninstall *DBA Dash Visualizer* from *Installed apps*.  That removes the app, its Start menu entry and any
*Open with* entries it added.

**Zip or winget:** if you registered it for files or added it to the Start menu, undo that first - run
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
3. **Installer.**  Run `Scripts/Pack-VisualizerInstaller.ps1 -Upload` on the corporate network, after step 2.  It takes the
   Visualizer zip from the draft release, packs it with [Velopack](https://velopack.io) (`vpk`), signs what identifies the app -
   `DBADashVisualizer.exe` if the zip didn't already have it signed, Velopack's launcher stub and updater (`Squirrel.exe`) and the
   setup - through `SignFiles.exe`, checks the signatures, and adds these to the draft release:
   `DBA_Dash_Visualizer_Setup_<version>.exe`, the `DBADashVisualizer-<version>-full.nupkg` package (and a `-delta.nupkg` against
   the previous release, if there is one), and the feed files `releases.win.json`, `RELEASES` and `assets.win.json` that installed
   copies read to find updates.  Velopack has to do the signing itself, part way through building, and the tool takes about half a
   minute a file, so it signs only those few files - the whole thing takes around 4 minutes.  Try it out without signing or
   uploading using `-ZipPath <zip> -SkipSigning`.
4. **Publish** the release.  Installed copies find the update from the feed of the latest published release.
5. **winget.**  Once the release is published, `Scripts/New-WingetManifest.ps1 -Version <version>` downloads the signed zip,
   works out its hash, writes the manifest under `DBADashBuild\winget` and validates it with `winget validate`.  Submit the
   result to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) as a pull request, or with
   [`wingetcreate submit`](https://github.com/microsoft/winget-create).  The script doesn't submit anything itself.

The winget package is still the zip.  Whether winget can also manage a copy from the setup program - it updates itself, and
would be updated by winget as well - hasn't been tried.

To try the installer channel before publishing, build both versions with `Pack-VisualizerInstaller.ps1 -ZipPath <zip>
-SkipSigning`, install the earlier setup, and set the environment variable `DBADASH_VISUALIZER_UPDATE_FEED` to the folder that
holds the later one's output before starting the app: it then looks there for updates instead of GitHub.

The code is in `DBADashVisualizer` (the app: start-up, updates, About) and `DBADash.Viewers.GUI` (the viewers, which the DBA
Dash GUI uses too).
