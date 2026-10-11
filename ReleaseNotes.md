## Which file should I download?

| I want to...                                                                    | Download                                                                     |
|-----------------------------------------------------------------------------------|-------------------------------------------------------------------------------|
| Set up monitoring (repository DB + service + GUI)                                 | [DBADash_{version}.zip](https://github.com/{repo}/releases/download/{version}/DBADash_{version}.zip) |
| Add a monitoring client on another machine (installer, updates to match the repository) | [DBADash_GUI_Setup_{version}.exe](https://github.com/{repo}/releases/download/{version}/DBADash_GUI_Setup_{version}.exe) |
| Add a monitoring client for all users of a machine (installer, needs admin)        | [DBADash_GUI_Setup_{version}.msi](https://github.com/{repo}/releases/download/{version}/DBADash_GUI_Setup_{version}.msi) |
| Add a monitoring client on another machine (xcopy deployment, no install)         | [DBADash_GUI_Only_{version}.zip](https://github.com/{repo}/releases/download/{version}/DBADash_GUI_Only_{version}.zip) |
| View query plans/deadlocks only - no monitoring (installer, auto-updates)         | [DBADash_Visualizer_Setup_{version}.exe](https://github.com/{repo}/releases/download/{version}/DBADash_Visualizer_Setup_{version}.exe) |
| View query plans/deadlocks only - for all users of a machine (installer, needs admin) | [DBADash_Visualizer_Setup_{version}.msi](https://github.com/{repo}/releases/download/{version}/DBADash_Visualizer_Setup_{version}.msi) |
| View query plans/deadlocks only - no monitoring (xcopy deployment, no install)     | [DBADash_Visualizer_{version}.zip](https://github.com/{repo}/releases/download/{version}/DBADash_Visualizer_{version}.zip) |

The GUI installer keeps the GUI at the version of the repository database it connects to - install the version that matches your repository.  The .msi installers ask whether to install for you or for everyone; installing updates for everyone needs admin rights.

All of the above also include the SSMS extension. See [Docs/Visualizer.md](Docs/Visualizer.md) and [Docs/GUIInstaller.md](Docs/GUIInstaller.md) for details.

## Installation

[Installation instructions](https://dbadash.com/installation)

[Upgrade instructions](https://dbadash.com/upgrades)

---