# DBA Dash GUI installer

The DBA Dash GUI is available as a setup program as well as a zip.  It's for people who run the GUI on their own machine,
connecting to a repository database that a DBA Dash service elsewhere maintains.  The service, and the full DBA Dash zip it
comes in, are unchanged.

## Install

**Setup program.**  Download `DBADash_GUI_Setup_<version>.exe` from the release that matches your repository database and run
it.  It installs for your user only, so it needs no administrator rights.  It adds DBA Dash to your Start menu and to
*Installed apps*, and installs the .NET Desktop Runtime if you don't have it.

**MSI.**  `DBADash_GUI_Setup_<version>.msi` asks whether to install for you only or for all users
of the machine.  See [Installing for all users](#installing-for-all-users).

**Zip.**  `DBADash_GUI_Only_<version>.zip`, extracted to a folder of your choice, as before.

## Updating

The GUI has to be on the same x.y version as the repository database.  A z release is a bug fix, with no repository changes
or only compatible ones.  An installed GUI never updates past the repository's x.y.z: it installs the version of the
repository it connects to, not the latest release.

- When the repository has been upgraded to a new x.y, the GUI offers to update to the repository's version as it connects.
  It downloads the update, closes, installs it and starts again.
- When only z differs, it doesn't ask.  **Upgrade** in the About box installs the repository's version, as long as it is
  newer than the GUI.
- When the GUI is on a newer x.y than the repository, it offers to install the repository's version in its place.
- If there is no installer for the repository's version (an older release, or one without the GUI installer), it says so, and
  the GUI carries on as it is.

If you connect to repositories on different versions, you are asked each time you switch between them.  One install holds
one version: use a zip copy for each version if you need them side by side.

A copy extracted from the zip is upgraded by the upgrade script, as before.

## Installing for all users

The setup program (`.exe`) always installs for the current user.  The `.msi` offers either:

- **Just me** - the same as the setup program.
- **Everyone** - installs under Program Files and needs administrator rights.  Updates are installed the same way, but they
  need administrator rights too: whoever connects to an upgraded repository is asked to elevate, and the update applies to
  every user of the machine.  So it suits a machine where the users all connect to the same repository, and that someone
  with admin rights looks after.

## Uninstall

From *Installed apps*.  This also removes the Open with entries for `.sqlplan` and `.xdl` files if they are for the installed
copy.  Your connections and settings are kept in `%APPDATA%\DBADashGUI` and in the GUI's user settings.

## For maintainers

The GUI installer is built from the signed GUI only zip, in the same way as the Visualizer's (see
[Visualizer.md](Visualizer.md#for-maintainers)), with `Scripts/Pack-Installer.ps1 -App GUI -Upload` after `SignRelease.ps1`.
It builds the .msi as well unless given `-NoMsi`.

Every release needs it: an installed GUI reads the feed of the release for its repository's version
(`https://github.com/trimble-oss/dba-dash/releases/download/<version>/releases.gui.json`), so a release without it can't be
installed or updated to.

The GUI is packed on its own Velopack channel, `gui`, so that its feed (`releases.gui.json`, `RELEASES-gui`,
`assets.gui.json`) doesn't overwrite the Visualizer's in the same release.  Its package id is `DBADash_GUI`: changing either
makes installed copies unable to find their updates.

To try it before publishing, pack two versions with `-ZipPath <zip> -SkipSigning`, install the earlier one, and set the
environment variable `DBADASH_GUI_UPDATE_FEED` to the folder that holds the later one's output before starting the GUI.  It
reads the feed there instead of GitHub, still taking only the repository's version from it.

The code is in `DBADashGUI\Updates\GuiInstaller.cs` (the prompts, download and install) and
`DBADash.Viewers.GUI\Updates\RepositoryVersionSource.cs` (the feed for one version).
