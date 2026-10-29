# Deploying Community Tools

DBA Dash can run popular community tools such as sp_WhoIsActive, the First Responder Kit and Erik Darling's procedures from the GUI.  The tools need to be installed on each monitored instance first, and DBA Dash can deploy and update them for you.

> **Disclaimer:** Community tools are third-party scripts written and maintained by their authors, not by DBA Dash.  They are provided under their own licenses and without warranty.  DBA Dash only fetches and deploys them - review each tool's license and documentation before using it in your environment.

## Tools

| Family | Tools | License |
|--------|-------|---------|
| [First Responder Kit](https://github.com/BrentOzarULTD/SQL-Server-First-Responder-Kit) | sp_Blitz, sp_BlitzFirst, sp_BlitzIndex, sp_BlitzCache, sp_BlitzWho, sp_BlitzLock, sp_BlitzBackups | MIT |
| [DarlingData](https://github.com/erikdarlingdata/DarlingData) | sp_HealthParser, sp_HumanEvents, sp_HumanEventsBlockViewer, sp_IndexCleanup, sp_LogHunter, sp_PressureDetector, sp_QuickieStore | MIT |
| [WhoIsActive](https://github.com/amachanic/sp_whoisactive) | sp_WhoIsActive | GPL-3.0 |
| [SQLStudies](https://github.com/sqlstudent144/SQL-Server-Scripts) | sp_SrvPermissions, sp_DBPermissions | MIT |

Run `DBADashConfig -a ListCommunityTools` for the version of each tool this build of DBA Dash is pinned to.

## Where the scripts come from

DBA Dash doesn't ship the scripts.  Each build pins a specific release of every tool, and `--Source` picks where they come from:

* **Pinned** (default) - downloads the pinned release from the author's GitHub repository and checks it against the SHA-256 hash recorded in DBA Dash.  The script isn't deployed if the hash doesn't match.
* **Latest** - downloads the newest release from GitHub.  These versions haven't been tested with DBA Dash and aren't hash checked.
* **Folder** - uses scripts you've downloaded to a folder, for servers without internet access.  Put the scripts either at their repository paths (e.g. `sp_HumanEvents\sp_HumanEvents.sql`, or a cloned or extracted repository) or directly in the folder.  sp_WhoIsActive has separate builds for older SQL versions (`2019\sp_WhoIsActive.sql` and `2008\sp_WhoIsActive.sql`) that share a file name, so those need the repository layout.

sp_WhoIsActive builds are picked automatically based on the SQL Server version.  Tools that don't support an instance (e.g. the First Responder Kit on versions before SQL Server 2016, or tools that don't support Azure SQL Database) are skipped.

The pins are kept current by a weekly GitHub workflow (`community-tools-update.yml`).  It checks each tool for a new release, deploys the new versions to a SQL Server 2022 test instance, checks their versions can be read, and raises a PR that updates the manifest.  The PR warns about parameters that were removed or changed type, and highlights the ones the GUI passes.  Maintainers can run the same step locally with `DBADashConfig -a UpdateCommunityToolsManifest --ManifestPath DBADashCommunityToolsCommunityToolsManifest.json -c "<test instance>"`.

## Which database

Every community tool is an `sp_` procedure.  When DBA Dash runs one, SQL Server looks in the database the connection lands in (its Initial Catalog, or the login's default database) and then in master.  Checking and deploying follow the same order, so they work with the copy DBA Dash actually runs:

* **Check** reports the copy in the connection's database if there is one, otherwise the copy in master.  If both exist, it notes that the one in master isn't used.
* **Deploy** updates a tool where it's already installed: in the connection's database, otherwise in master.  A tool that isn't installed anywhere goes in the connection's database.  This stops an update adding a second copy to the user database that silently takes over from the one in master.

Use `--Database` (or Database in the service configuration tool) to check or deploy a specific database instead.  The results say if DBA Dash would use a different copy.

If the connection has no Initial Catalog, "the database the connection lands in" is the default database of the login that deploys: you for Windows authentication, or the login from `--SqlUser` or Connect as.  That may not be the default database of the login DBA Dash runs as.  Set Initial Catalog on the connection, or specify the database.  (Tools already installed in master are updated there either way.)

On Azure SQL Database there's no lookup in master, so the tools can only be deployed to the database DBA Dash connects to.

## Security

Deployment connects as **you**, not as the DBA Dash service account.  Creating procedures needs permissions that the service account shouldn't have.

* For connections using Windows authentication, the tool connects as the Windows user running it.
* Use `--SqlUser` to connect with a SQL login instead.  You're prompted for the password, or it's read from the `DBADASH_PASSWORD` environment variable (`--SqlPassword` also works, but leaves it in your shell history).  This is needed for connections configured with SQL authentication, as those credentials are usually the low privilege login DBA Dash monitors with.

The account needs permission to create and alter procedures in the target database (e.g. db_owner on master, or sysadmin).

Checking versions only needs VIEW DEFINITION.  Versions are read from each procedure's definition - DBA Dash doesn't execute the tools to check them.

To run the tools from the GUI, they also need to be allowed in the service config (`--AddToAllowedScripts`, or the service configuration tool), and the service account needs EXECUTE permission on them (see the permissions helper in the service configuration tool).

## Service configuration tool

Click **Community Tools** under the existing connections (or **Deploy Tools...** next to Allowed Scripts on the Messaging tab).

1. Pick the tools and instances.  The tools already in Allowed Scripts are ticked to start with.
2. **Check Versions** shows what's installed on each instance and whether it's up to date.
3. Choose the source and database if you don't want the defaults.
4. **Connect as** uses each connection's own credentials by default.  Click it to enter different credentials for all the selected instances - for example your Windows login, or an admin SQL login.
5. Tick the acknowledgement and click **Deploy**.

After deploying you're offered to add the tools to Allowed Scripts and to open the Permissions Helper to grant the service account EXECUTE on them.  Save the service configuration afterwards to apply the Allowed Scripts change.

## Command line

List the tools:

```
DBADashConfig -a ListCommunityTools
```

Check what's installed on every instance in the config:

```
DBADashConfig -a CheckCommunityTools --ConnectionIDs *
```

Deploy sp_WhoIsActive and the First Responder Kit to two instances:

```
DBADashConfig -a DeployCommunityTools --Tools "sp_WhoIsActive,FirstResponderKit" --ConnectionIDs "SQL1,SQL2" --AcceptDisclaimer
```

Deploy everything from a local folder using a SQL login, and allow the tools to be run from the GUI:

```
DBADashConfig -a DeployCommunityTools --ConnectionIDs * --Source Folder --SourceFolder C:\CommunityTools --SqlUser deploy_user --AcceptDisclaimer --AddToAllowedScripts
```

| Option | Description |
|--------|-------------|
| `--Tools` | Comma-separated tools and/or families.  `*` (default) for all. |
| `-c` / `--ConnectionID` / `--ConnectionIDs` | A connection string, one ConnectionID from the config, or a comma-separated list of ConnectionIDs (`*` for all). |
| `--Source` | `Pinned` (default), `Latest` or `Folder`. |
| `--SourceFolder` | Folder for `--Source Folder`. |
| `--Database` | Database to deploy to or check.  Default is master. |
| `--SqlUser` / `--SqlPassword` | Connect with a SQL login instead of the connection's own credentials.  The password is prompted for if `--SqlPassword` and `DBADASH_PASSWORD` aren't set. |
| `--AcceptDisclaimer` | Required to deploy. |
| `--AddToAllowedScripts` | Add the deployed tools to AllowedScripts in the service config. |

The command exits with code 1 if any deployment fails.
