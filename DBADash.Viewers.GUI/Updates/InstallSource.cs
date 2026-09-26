namespace DBADashGUI.Viewers
{
    /// <summary>How this copy of the app was installed, which decides how it is updated.</summary>
    public enum InstallSource
    {
        /// <summary>Extracted from the release zip.  Updated by extracting a newer zip over it.</summary>
        Zip,

        /// <summary>Installed by winget.  Updated by <c>winget upgrade</c>.</summary>
        Winget
    }

    /// <summary>Works out where a copy of the app came from.</summary>
    public static class InstallLocation
    {
        /// <summary>
        /// Winget unzips a portable package into a folder of its own under a WinGet\Packages folder -
        /// %LOCALAPPDATA%\Microsoft\WinGet\Packages for the user, or Program Files\WinGet\Packages for the machine - and puts a
        /// link to the executable in WinGet\Links.  A copy running from either was put there by winget.
        /// </summary>
        public static InstallSource Detect(string appDirectory)
        {
            if (string.IsNullOrWhiteSpace(appDirectory)) return InstallSource.Zip;

            // Directory separators either way round, and a folder without the trailing one.
            var path = appDirectory.Replace('/', '\\').TrimEnd('\\') + "\\";

            return path.Contains(@"\WinGet\Packages\", StringComparison.OrdinalIgnoreCase) ||
                   path.Contains(@"\WinGet\Links\", StringComparison.OrdinalIgnoreCase)
                ? InstallSource.Winget
                : InstallSource.Zip;
        }
    }
}
