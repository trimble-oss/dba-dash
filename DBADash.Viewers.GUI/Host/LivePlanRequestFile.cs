using DBADashGUI.ShellIntegration;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace DBADashGUI.Viewers
{
    /// <summary>
    /// A request from the SSMS extension to follow a running query's live plan: which session, the request's start time,
    /// and the connection the SSMS query window it came from was using.  Written to %TEMP% as ssms_*.dbadashlive and
    /// passed on the command line, the same way the extension hands over plans and grids.<br/>
    /// The whole file is protected with DPAPI for the current Windows user, so nothing in it - the connection string
    /// included, whatever it holds - is plain text on disk, and only a file this user protected on this machine is read
    /// at all.  A SQL login's password is protected again inside it, so it is never a string once read.  The file is
    /// deleted as soon as it has been read, and nothing from it is ever written to the command line.<br/>
    /// Only a file the extension wrote is accepted - in %TEMP%, named as it names them, recent, and protected for this
    /// user: the file says where to connect, so one from anywhere else could send this user's Windows credentials to a
    /// server of someone else's choosing.  The extension isn't a file association or in the Open dialog for the same reason.
    /// </summary>
    public sealed class LivePlanRequestFile
    {
        public const string Extension = ".dbadashlive";

        // Shared with the extension's VisualizerLauncher. Scope the protected password, and the protected file, to this use.
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("DBADash.LivePlanRequest");
        private static readonly byte[] FileEntropy = Encoding.UTF8.GetBytes("DBADash.LivePlanRequest.File");

        internal const int CurrentVersion = 2;

        // A request is a few hundred bytes. Anything much bigger isn't one, and isn't read into memory to find out.
        private const long MaxFileLength = 64 * 1024;

        // The extension launches the app straight after writing the file. Anything older wasn't meant for now.
        private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(10);

        public string ConnectionString { get; private init; }

        /// <summary>The SQL login's password, read only, or null for Windows authentication.</summary>
        public SecureString Password { get; private init; }

        public int SessionId { get; private init; }

        /// <summary>The request's start time as SSMS showed it - the instance's local time.  Null to follow whatever the session is running.</summary>
        public DateTime? StartTime { get; private init; }

        /// <summary>Where it came from - the SSMS query window's document - for the tab.</summary>
        public string Title { get; private init; }

        public static bool IsRequestFile(string path) => Path.GetExtension(path).Equals(Extension, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Reads the request and deletes the file.  Throws if it isn't a request the SSMS extension has just written.  A
        /// file anywhere else is refused without being touched - it isn't the extension's to delete - while one of the
        /// extension's is deleted whether or not it could be read, as it may hold a protected password.
        /// </summary>
        public static LivePlanRequestFile ReadAndDelete(string path)
        {
            if (!SsmsExtensionInstaller.IsFromExtension(path))
            {
                throw new InvalidOperationException("Live plan requests are only accepted from the DBA Dash SSMS extension.");
            }

            try
            {
                if (DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > MaxAge)
                {
                    throw new InvalidOperationException("This live plan request has expired.  Open the live plan from SSMS again.");
                }

                if (new FileInfo(path).Length > MaxFileLength) throw new InvalidDataException("Not a DBA Dash live plan request.");

                return Parse(UnprotectFile(File.ReadAllBytes(path)));
            }
            finally
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                    // The extension sweeps its old temp files too.
                }
            }
        }

        /// <summary>
        /// The request's XML, from the file's DPAPI-protected bytes - wiped once parsed.  A file that isn't protected for
        /// this user with this entropy - one written by an older extension, by another user, or not by the extension at
        /// all - doesn't decrypt, and is refused.
        /// </summary>
        private static XDocument UnprotectFile(byte[] protectedFile)
        {
            byte[] plain = null;
            try
            {
                plain = ProtectedData.Unprotect(protectedFile, FileEntropy, DataProtectionScope.CurrentUser);
                using var stream = new MemoryStream(plain, writable: false);
                // XDocument.Load's reader prohibits DTDs, so the XML can't pull in anything else.
                return XDocument.Load(stream);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidDataException("This live plan request could not be read.  It can only be read by the Windows user who opened it from SSMS, with the same version of the SSMS extension as DBA Dash.", ex);
            }
            finally
            {
                if (plain != null) CryptographicOperations.ZeroMemory(plain);
            }
        }

        internal static LivePlanRequestFile Parse(XDocument document)
        {
            var root = document.Root;
            if (root?.Name.LocalName != "DBADashLivePlan") throw new InvalidDataException("Not a DBA Dash live plan request.");
            if ((int?)root.Attribute("Version") != CurrentVersion)
            {
                throw new InvalidDataException("This live plan request is from a different version of the SSMS extension.  Update DBA Dash and the extension to the same version.");
            }

            var connectionString = (string)root.Element("ConnectionString");
            if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidDataException("The live plan request has no connection.");

            var sessionId = (int?)root.Element("SessionId") ?? throw new InvalidDataException("The live plan request has no session.");

            var password = (string)root.Element("Password");
            return new LivePlanRequestFile
            {
                ConnectionString = connectionString,
                Password = string.IsNullOrEmpty(password) ? null : UnprotectPassword(password),
                SessionId = sessionId,
                StartTime = (DateTime?)root.Element("StartTime"),
                Title = (string)root.Element("Title")
            };
        }

        /// <summary>
        /// The password, straight into a read-only SecureString - what SqlCredential takes - rather than a string, and
        /// the decrypted bytes and characters wiped once copied.
        /// </summary>
        private static SecureString UnprotectPassword(string protectedPassword)
        {
            byte[] bytes = null;
            char[] chars = null;
            try
            {
                bytes = ProtectedData.Unprotect(Convert.FromBase64String(protectedPassword), Entropy, DataProtectionScope.CurrentUser);
                chars = Encoding.UTF8.GetChars(bytes);

                var password = new SecureString();
                foreach (var c in chars) password.AppendChar(c);
                password.MakeReadOnly();
                return password;
            }
            catch (CryptographicException ex)
            {
                throw new InvalidDataException("The live plan request's password could not be read.  It can only be read by the Windows user who opened it from SSMS.", ex);
            }
            finally
            {
                if (bytes != null) CryptographicOperations.ZeroMemory(bytes);
                if (chars != null) Array.Clear(chars);
            }
        }
    }
}
