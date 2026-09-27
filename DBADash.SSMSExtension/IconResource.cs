using System.Drawing;
using System.Reflection;

namespace DBADash.SSMSExtension
{
    /// <summary>The DBA Dash app icon, embedded for the context menu this extension builds itself
    /// (DeadlockContextMenuHook) - the .vsct-declared commands get theirs from the compiled bitmap strip
    /// instead, so this is only needed outside that mechanism.</summary>
    internal static class IconResource
    {
        public static readonly Image DBADash = Load();

        private static Image Load()
        {
            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DBADashIcon.png");
                if (stream == null) return null;

                // Copied into a Bitmap of its own: an Image from FromStream needs its stream kept open for
                // as long as the Image lives, and this one is a static held for the life of SSMS.
                using var image = Image.FromStream(stream);
                return new Bitmap(image);
            }
            catch
            {
                return null;
            }
        }
    }
}
