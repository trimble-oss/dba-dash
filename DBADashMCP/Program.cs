using DBADashAI.Mcp;
using DBADashAI.Services;
using DBADashMCP;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

// GenerateAssemblyInfo is off (the version comes from GlobalAssemblyInfo.cs), so the SDK does not add this for
// the -windows target.  The saved GUI connections are DPAPI-encrypted, which is Windows only.
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]

// A local MCP server over stdio, for Claude Code and other MCP clients.  The client starts it:
//
//   claude mcp add dbadash -- "C:\Program Files\DBA Dash\DBADashMCP.exe"
//
// It reads the repository with the user's own GUI connection, so it needs no DBA Dash AI service and
// sees only what that user is allowed to.  stdout is the protocol channel: everything else, logging
// included, goes to stderr.

string? connectionName = null;
string? connectionString = null;
string? dataDetail = null;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--connection" when i + 1 < args.Length:
            connectionName = args[++i];
            break;
        case "--connection-string" when i + 1 < args.Length:
            connectionString = args[++i];
            break;
        case "--data-detail" when i + 1 < args.Length:
            dataDetail = args[++i];
            if (!AiDataMask.TryParse(dataDetail, out _))
            {
                await Console.Error.WriteLineAsync($"--data-detail must be one of: {string.Join(", ", AiDataMask.Words)}.");
                return 1;
            }
            break;
        case "--list-connections":
            foreach (var saved in RepositoryConnectionResolver.GetSavedConnections())
            {
                Console.WriteLine(saved.IsDefault ? $"{saved.Name} (default)" : saved.Name);
            }
            return 0;
        case "--help" or "-h" or "/?":
            Console.WriteLine("""
                DBADashMCP - a Model Context Protocol server over the DBA Dash repository, for MCP clients
                such as Claude Code.  The client runs it; it talks MCP over stdin/stdout.

                  --connection <name>          Use the saved GUI repository connection with this name.
                  --connection-string <value>  Use this connection string instead of a saved connection.
                  --list-connections           List the saved GUI repository connections and exit.
                  --data-detail <level>        What is masked before results reach the model, fixed for as long
                                               as the server runs.  Without it, the server follows the setting in
                                               the DBA Dash GUI's Options > Connect AI Tools, read on each call:
                                                 parametersAndLiterals (default)  parameter values and the literals
                                                                                  in statement text and plans
                                                 parameters                       parameter values only
                                                 literals                         literals only
                                                 full                             nothing
                                                 metadataOnly                     statement text, plans and input
                                                                                  buffers withheld; logins, hosts and
                                                                                  application names coded

                With no options it uses the default repository connection saved in the DBA Dash GUI.

                  claude mcp add dbadash -- "<path>\DBADashMCP.exe"
                """);
            return 0;
        default:
            await Console.Error.WriteLineAsync($"Unknown argument '{args[i]}'.  Run DBADashMCP --help for the options.");
            return 1;
    }
}

string repository;
try
{
    repository = RepositoryConnectionResolver.Resolve(connectionString, connectionName);
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync(ex.Message);
    return 1;
}

// No host defaults: the exe shares a folder with the AI service, whose appsettings.json is not for it - its
// logging section alone would put a line per request on stderr, which the client shows in its own logs.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    ContentRootPath = AppContext.BaseDirectory,
    DisableDefaults = true
});

builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "DBADashMCP.json"), optional: true);
builder.Configuration["ConnectionStrings:Repository"] = repository;
// The command line wins over DBADashMCP.json; either fixes the masking.  With neither, the server follows the user's
// setting in the GUI, read on each call - an AI tool's session, and this server with it, can run for days.
if (dataDetail is not null)
{
    builder.Configuration[AiDataMask.SettingName] = dataDetail;
}
var followUserSetting = string.IsNullOrWhiteSpace(builder.Configuration[AiDataMask.SettingName]);
// The client and this server run as the same user on the same machine, so reading a file the user names is no more
// than the client could do itself - unlike on the AI service.
builder.Configuration[McpToolRegistration.AllowFilePathsSetting] ??= "true";
// An unrecognised value in DBADashMCP.json is a startup error, not a silent fall back to full detail.
AiDataMask dataMask;
try
{
    dataMask = AiDataMask.FromConfiguration(builder.Configuration);
}
catch (InvalidOperationException ex)
{
    await Console.Error.WriteLineAsync(ex.Message);
    return 1;
}

builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services
    .AddMcpServer(options => McpToolRegistration.Configure(options, typeof(Program).Assembly.GetName().Version?.ToString(),
        followUserSetting ? AiDataMaskSource.FromUserSetting() : AiDataMaskSource.Fixed(dataMask)))
    .WithStdioServerTransport();

builder.Services
    .AddAiTools()
    .AddMcpTools(guiContext: true);

// Local only: these read the GUI open in this Windows session, and the prompts above start from what it has open.
builder.Services.AddHostedService<ParentProcessWatcher>();
builder.Services.AddSingleton<GuiContextReader>();
builder.Services.AddSingleton<McpServerTool, GuiContextTool>();
builder.Services.AddSingleton<McpServerTool, GetOpenDeadlockTool>();
builder.Services.AddSingleton<McpServerTool, GetOpenQueryPlanTool>();

await builder.Build().RunAsync();
return 0;
