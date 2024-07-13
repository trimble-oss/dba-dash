using CommandLine;

namespace DBADashService
{
    [Verb("install", HelpText = "Install the service")]
    internal class InstallOptions
    {
        [Option("autostart", Required = false, HelpText = "The service should start automatically")]
        public bool AutoStart { get; set; }

        [Option("disabled", Required = false, HelpText = "The service should be set to disabled")]
        public bool Disabled { get; set; }

        [Option("manual", Required = false, HelpText = "The service should be started manually")]
        public bool Manual { get; set; }

        [Option("delayed", Required = false, HelpText = "The service should start automatically (delayed)")]
        public bool Delayed { get; set; }

        [Option("username", Required = false, HelpText = "Sets the username for the service account.")]
        public string Username { get; set; }

        [Option("localsystem", Required = false, HelpText = "Run the service with the LocalSystem account.")]
        public bool LocalSystem { get; set; }

        [Option("localservice", Required = false, HelpText = "Run the service with the LocalService account.")]
        public bool LocalService { get; set; }

        [Option("networkservice", Required = false, HelpText = "Run the service with the NetworkService account.")]
        public bool NetworkService { get; set; }

        [Option("password", Required = false, HelpText = "Sets the password for the service account.")]
        public string Password { get; set; }
    }

    [Verb("start", HelpText = "Start the service")]
    internal class StartOptions
    {
    }

    [Verb("stop", HelpText = "Stop the service")]
    internal class StopOptions
    {
    }

    [Verb("uninstall", HelpText = "Uninstall the service")]
    internal class UninstallOptions
    {
    }

    [Verb("run", true, HelpText = "Run collections")]
    internal class RunOptions
    {
    }
}