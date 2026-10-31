# MCP server (Claude Code and other MCP clients)

DBA Dash can act as a [Model Context Protocol](https://modelcontextprotocol.io) server. With it, Claude Code, Claude Desktop, VS Code and other MCP clients can query your DBA Dash repository using their own model. The built-in [AI Assistant](AI-Assistant.md) is not involved.

The MCP server offers the same 27 repository tools that the AI Assistant uses to answer questions. Examples include active alerts, waits, blocking, slow queries, backups, drives, configuration drift and capacity forecasts. It also has tools that fetch a single deadlock, query plan or instance's performance in detail, plus prompts that run the same analyses as the GUI. Your client's model reads each tool's description, chooses the tools it needs and fills in their arguments. DBA Dash never calls a model itself, so you don't need to configure an AI provider.

The tools are read-only. They read summaries from the repository and never connect to the monitored instances.

There are two ways to run it:

| | Local (`DBADashMCP.exe`) | Shared (`/mcp` on the AI service) |
|---|---|---|
| Needs the AI service | No | Yes |
| Transport | stdio (the client starts the exe) | Streamable HTTP |
| Repository identity | The user running the client | The AI service's account |
| Authentication | The user's own SQL permissions | The AI service API key |

## Local: DBADashMCP.exe

`DBADashMCP.exe` is installed alongside the DBA Dash GUI. By default it uses the repository connection you saved as the default in the GUI. The connection details are read from `%AppData%\DBADashGUI\ConnectionStrings.json` and are encrypted for your Windows user. If you haven't saved a connection in the GUI, it uses `ServiceConfig.json` from the same folder as the exe.

### Connect it from the GUI

In the GUI, go to **Options > Connect AI Tools (MCP)**. The dialog lists Claude Code, Claude Desktop, Cursor, VS Code (Copilot agent mode) and Windsurf. For each one, it shows whether DBA Dash is registered, and lets you add, update or remove it with one click.

- **Repository connection:** choose the saved GUI connection the AI tools should use. The current one is selected.
- **Access check:** the dialog checks that your login can use the tools on that repository. If it can't, it shows the `ALTER ROLE McpUser ADD MEMBER` statement for a DBA to run.
- **Claude Code** is registered through its own `claude mcp add` command, for your user. **The others** have their JSON config file edited. Other servers and settings in that file are kept, and the original is saved beside it as `.dbadash-backup`.
- **Update** appears when a tool is registered with a different copy of DBA Dash or a different connection, for example after DBA Dash moved.
- **Copy config JSON** copies the usual `mcpServers` snippet, for any other AI tool that takes MCP servers.

After adding it, restart the tool or start a new session so it picks the server up.

### Ask an AI tool from the GUI

The GUI shows an **Ask** button (a sparkle icon) beside **AI Analysis** in the deadlock viewer, the query plan viewer, and the Performance, Performance Summary, Latches and Spinlocks tabs. Its menu hands the deadlock, the plan statement, or the instance over the tab's dates to an AI tool. On Performance Summary, that's the instance selected in the grid. Each menu item shows the icon of the tool installed on your machine; DBA Dash doesn't ship their logos. The tool answers under the same headings as the AI Assistant's own analysis, and you can carry on the conversation there.

The menu lists the tools available on your machine:

| Menu item | When it's shown | What it does |
|---|---|---|
| Ask Claude Code | Claude Code is installed | Opens Claude Code in a new window. It needs nothing registered, because it brings its own copy of the server (see below). |
| Ask Claude Code in the Claude app | The Claude desktop app is installed, and DBA Dash is registered with Claude Code | Opens a Claude Code session in the desktop app with the prompt filled in. The app asks you to confirm the folder each time, then you press send. It uses the server registered with Claude Code, since a link can't pass one. |
| Ask Cursor | DBA Dash is registered with Cursor | Opens Cursor with the prompt filled in. Cursor never runs a prompt from a link by itself, so press send. |
| Ask VS Code | DBA Dash is registered with VS Code, and its `code` command is installed | Starts a Copilot agent mode chat with the request attached. |
| Copy prompt | Any of the above, or DBA Dash is registered with another tool | Copies the prompt, to paste into Claude Desktop, Windsurf or any other tool with DBA Dash's MCP server. |

If none of these is available, the button isn't shown. **Options > Connect AI Tools** registers DBA Dash with the tools. The menu checks again each time it opens, so a tool you register while the GUI is running appears straight away.

Whichever tool you pick, the GUI prepares a DBA Dash workspace folder, `%LOCALAPPDATA%\DBADash\ClaudeCode`, the same way each time:

- `dbadash.mcp.json` starts DBA Dash's MCP server with the GUI's current repository connection, for Claude Code. Its session uses only this server, so a `dbadash` you registered yourself, with another copy or connection, doesn't get in the way.
- `.claude/settings.json` allows the server's tools, which only read, so Claude Code doesn't ask before each one.
- `CLAUDE.md` tells Claude Code what DBA Dash is and how the folder works.
- `artifacts/` holds the deadlock graph or plan as a file, so the answer is about the one you clicked on, even if you've moved on in the GUI by the time the tool reads it.
- `requests/` holds the full prompt, the same one the `analyse-*` prompts give.

Cursor, VS Code and pasted prompts use the `dbadash` server registered with that tool, so they read the artifact through the local `DBADashMCP.exe`. Files in `artifacts/` and `requests/` are removed after a week. The first time Claude Code opens the folder, it asks whether you trust it.

### Server processes

Each AI tool session starts its own `DBADashMCP.exe`, so several can be running at once while several sessions are open. Each one stops when its session ends, and also when the process that started it exits, so a crashed tool doesn't leave one behind.

Upgrades stop any copies running from the install folder before replacing files: the GUI's own update, `UpgradeDBADash.ps1`, and the config tool's upgrade. The AI tool starts the server again when it's next used. Some tools need you to reconnect it, such as `/mcp` in Claude Code.

### Connect it by hand

Add it to Claude Code:

```bash
claude mcp add dbadash -- "C:\Program Files\DBA Dash\DBADashMCP.exe"
```

Options:

| Option | Description |
|---|---|
| `--connection <name>` | Use the saved GUI connection with this name instead of the default. |
| `--connection-string <value>` | Use this connection string instead of a saved connection. |
| `--list-connections` | List the saved GUI connections, then exit. |

For example, to point at a specific repository:

```bash
claude mcp add dbadash -- "C:\Program Files\DBA Dash\DBADashMCP.exe" --connection "SQL01\DBADashDB"
```

### Permissions

The local server connects to the repository as you. Members of `db_owner` already have the access it needs. For other users, add them to the `McpUser` database role:

```sql
ALTER ROLE McpUser ADD MEMBER [DOMAIN\User];
```

`McpUser` can execute the procedures the tools call. For the performance tool it also has SELECT on the `dbo` schema, because several of the performance chart procedures build dynamic SQL. That is the same read access the `App` role gives GUI users. `McpUser` has no access to the AI service's configuration, API keys or stored analyses. Its sessions show `DBADashMCP` as their application name.

### What you're looking at in the GUI

The local server can see what the DBA Dash GUI is showing, through three more tools:

| Tool | What it returns |
|---|---|
| `get-gui-context` | The selected tree node and its instance, database or object, the open tab, and the time window. It also says whether a deadlock or plan is open in the GUI's viewers. You can then ask Claude Code about "this instance" or "the selected time range", and it gets the instance name and `hoursBack` value to pass to the other tools. |
| `get-open-deadlock` | The deadlock open in the GUI's deadlock viewer, in the same detail as `get-deadlock`. |
| `get-open-query-plan` | The plan open in the GUI's plan viewer, with the statement selected in it, in the same detail as `get-query-plan`. |

The GUI sends the open deadlock graph or plan itself, not just a key, so these tools also work for a deadlock or plan that was never in the repository: one opened from a file, a Query Store plan, or a live plan. Object definitions for a deadlock are only looked up when the GUI knows which instance it came from. If several viewer windows are open, the tools use the selected tab of the viewer window you used last.

The GUI publishes this on a named pipe that only your Windows user, in your logon session, can open. Each request gets a fresh snapshot. If you have more than one copy of the GUI open, the first one started answers. The tool also warns you when the GUI is connected to a different repository from the MCP server.

## Shared: the AI service's /mcp endpoint

The [AI service](AI-Assistant.md) can serve the same tools over HTTP at `/mcp`. How it is enabled depends on the deployment mode:

- **Local mode** (loopback only, no authentication): the endpoint is on by default.
- **Shared mode**: set `"Mcp": { "Enabled": true }` in `appsettings.json`. The endpoint then needs the same `X-API-Key` header as the rest of the API.

Anyone with the API key can then read the repository summaries through the AI service's own account.

The AI service's `AIService` role covers the summary tools, `list-deadlocks`, `get-deadlock` and `get-query-plan`. Without more access, `get-deadlock` returns no object definitions, and `get-performance-summary` fails with a permission error. To enable both, add the AI service's database user to `McpUser`.

```bash
claude mcp add --transport http dbadash http://localhost:5055/mcp
```

```bash
claude mcp add --transport http dbadash http://aiserver:5055/mcp --header "X-API-Key: <key>"
```

## Tool arguments

Every summary tool takes the same three optional arguments:

| Argument | Description |
|---|---|
| `instance` | Only instances whose DBA Dash display name starts with this text. |
| `hoursBack` | How many hours of history to look at, for tools that look back. Defaults to the tool's own window, usually 24 hours. |
| `maxRows` | The maximum number of rows returned from each result set, from 1 to 200. Defaults to 50. |
| `to` | When the window ends, in UTC. Leave it out for now. Use it to investigate a past incident: for example, `hoursBack` 2 and `to` 2026-10-23T03:00:00 cover 01:00 to 03:00 that night. Only tools with a time window take it: waits, blocking, slow queries, running queries, deadlocks, failed logins, OS performance, workload pressure and agent jobs. |

When a list comes back exactly `maxRows` long, the result says so, because there are probably more rows. Figures are rounded to 3 decimal places.

## Deadlocks, query plans and performance

These tools return the same data the GUI's AI analyses send to the AI Assistant. They use the same parsing and rules as the deadlock viewer, the plan viewer and the performance analysis dialog. All times are UTC.

| Tool | What it returns |
|---|---|
| `list-deadlocks` | The deadlocks captured by the Deadlocks collection, newest first, with each victim's details. It takes the summary arguments above. |
| `get-deadlock` | One deadlock, by the `instanceId`, `eventTime` and `deadlockHash` values from `list-deadlocks`. It returns the participants, the objects and modules involved, the findings from DBA Dash's deadlock rules, and the object definitions as they were when the deadlock happened (from schema snapshots). It also returns the deadlock graph. |
| `top-queries` | The queries that used the most CPU over a window, from the Query Stats collection. It shows the same figures as the GUI's Query Stats tab, because it uses the same procedure. It returns executions, CPU, duration, reads, writes, spills and memory grants, and each query's share of the instance's CPU. Rows are query families by default; set `groupBy` to `statement` or `plan` instead. A coverage section says how much of the window was collected and how much work couldn't be attributed. It takes `to` too. |
| `get-query-plan` | A captured plan, by `queryPlanHash`. `top-queries` and `running-queries-summary` list the hashes. It returns the statement, plan context and statistics, plan warnings, missing indexes, and operators ranked by their own cost. Set `includePlanXml` to add the showplan XML. If the XML would exceed the output limit, it is left out rather than cut. |
| `get-performance-summary` | One instance's CPU, waits, IO, counters, memory, blocking, object execution, latches and spinlocks over a window of up to 7 days. It is compared with the same window a week earlier. Set `focus` to `latches-and-spinlocks` for contention detail. |
| `get-root-blockers` | The root blockers in an instance's worst blocking snapshots over a window: what each was running, its application and login, and whether it was sleeping with a transaction open. It also shows what the blocked sessions waited on. |
| `get-waiting-queries` | The queries seen waiting on a wait type, given as `waitType`, such as `PAGELATCH%` or `LCK_M%`. Set `tempdbOnly` for waits on tempdb pages only. |
| `get-long-running-queries` | The longest running queries and longest open transactions seen over a window. |
| `get-deadlock-signature` | One deadlock pattern: how often its signature occurred in the window, and the processes and resources of its latest occurrences. |
| `describe-deadlock-graph` | A deadlock graph that isn't in the repository, in the same detail as `get-deadlock`: pass the XML, or on the local server the path of a `.xdl` file. Give `instance` to look up the object definitions from its schema snapshots. When the XML holds several deadlocks, `graph` picks one. |
| `describe-query-plan` | A plan that isn't in the repository, in the same detail as `get-query-plan`: pass the showplan XML, or on the local server the path of a `.sqlplan` file. A plan with several statements is described one statement at a time, listing the others so you can ask for them by number. |

The four tools from `get-root-blockers` to `get-deadlock-signature` are the lookups the AI Assistant's performance analysis can make, on the same procedures. Each needs `instance` and takes `hoursBack` (up to 7 days) and `to`. Narrow them to a peak in `get-performance-summary` to see what was behind it. They read the running queries snapshots, so a duration is how long something had been running when the snapshot was taken. The `analyse-performance` prompt tells the model to use them before naming the cause of blocking, tempdb contention or a deadlock.

The AI service's `/mcp` endpoint doesn't read files, because the path would be on the service's machine and read with the service's account. Pass the XML instead. Set `Mcp:AllowFilePaths` to change this.

### Prompts

The server also offers three prompts. In Claude Code they appear as slash commands:

| Prompt | Does |
|---|---|
| `/mcp__dbadash__analyse-deadlock` | Analyses a deadlock: what happened, why, what to do and what to check next. |
| `/mcp__dbadash__analyse-query-plan` | Analyses a captured plan: what it does, where the time goes, what to do and what to check next. |
| `/mcp__dbadash__analyse-performance` | Analyses an instance's performance against the baseline, using the same guidance as the GUI's performance analysis. |

Which deadlock or plan a prompt analyses depends on how you run it:

- **With no arguments, on the local server:** the prompt picks the first of these that exists:
  1. The deadlock or plan open in the GUI.
  2. The most recent deadlock, or the heaviest query's plan, on the instance and in the time window the GUI shows.
  3. The most recent deadlock, or the heaviest query's plan, on any instance.

  The answer starts by saying which deadlock or plan it picked, and why.
- **With no arguments, on the shared `/mcp` endpoint:** the AI service can't see your GUI, so it picks from the most recent across all instances.
- **With an instance:** for example `/mcp__dbadash__analyse-deadlock SQL01`. The prompt uses that instance and ignores the GUI. Arguments are passed by position, in the order the prompt lists them.

`analyse-performance` follows the same pattern. On the local server, with no instance given, it uses the instance and time window the GUI shows.

The prompts use the same headings and guidance as the AI Assistant's built-in analyses. The difference is that your client's model fetches the data itself.

## Output size

Results are capped so they don't fill the client's context window. When a result is trimmed, the result says so, and the model can ask again for a single instance or a shorter window.

| Setting | Default | Description |
|---|---|---|
| `Mcp:MaxOutputChars` | 60000 | The maximum size of a tool result, in characters. |
| `Mcp:MaxStringValueLength` | 2000 | Longer text values, such as query text, are truncated to this length. |

For the AI service, these settings go in `appsettings.json`. For `DBADashMCP.exe`, put them in a `DBADashMCP.json` file in the same folder as the exe.

## Data leaving DBA Dash

Tool results go to whichever model your MCP client uses. That model is outside the AI provider you configured for the AI Assistant. Results can include instance names, query text, login names and client hosts. Deadlock graphs and query plans can also contain parameter values, and `get-deadlock` includes object definitions. Use a client and model that you are allowed to send that data to.
