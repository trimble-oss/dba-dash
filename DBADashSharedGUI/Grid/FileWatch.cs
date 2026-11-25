using System;
using System.IO;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// Watches a file, and raises <c>changed</c> when its last write time or length differs from the last time it was
    /// read - checked every couple of seconds, on the UI thread.  The host reads the file again when it's told, and
    /// calls <see cref="NoteRead"/> with the stamp taken before that read, so a change made during it is still seen.
    /// </summary>
    public sealed class FileWatch : IDisposable
    {
        /// <summary>How often the file is checked.</summary>
        public const int DefaultIntervalMilliseconds = 2000;

        private readonly Action _changed;
        private readonly System.Windows.Forms.Timer _timer;
        private (DateTime WriteUtc, long Length)? _stamp;
        private bool _wanted;

        public FileWatch(Action changed, int intervalMilliseconds = DefaultIntervalMilliseconds)
        {
            _changed = changed;
            _timer = new System.Windows.Forms.Timer { Interval = intervalMilliseconds };
            _timer.Tick += (_, _) => Check();
        }

        /// <summary>The file being watched, or null.</summary>
        public string Path { get; private set; }

        /// <summary>Whether the file is checked.  Stays on or off across <see cref="Start"/>, so it can be set first.</summary>
        public bool Enabled
        {
            get => _wanted;
            set
            {
                _wanted = value;
                _timer.Enabled = _wanted && Path is not null;
            }
        }

        /// <summary>Watches <paramref name="path"/>, as it was when read with <paramref name="stamp"/>.</summary>
        public void Start(string path, (DateTime WriteUtc, long Length) stamp)
        {
            Path = path;
            _stamp = stamp;
            _timer.Enabled = _wanted && Path is not null;
        }

        /// <summary>Notes the file has been read as it is with <paramref name="stamp"/>.</summary>
        public void NoteRead((DateTime WriteUtc, long Length) stamp) => _stamp = stamp;

        /// <summary>The file's last write time and length, or null when it isn't there - what a change is judged by.</summary>
        public static (DateTime WriteUtc, long Length)? Stamp(string path)
        {
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists) return null;
                return (info.LastWriteTimeUtc, info.Length);
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        private void Check()
        {
            if (Path is null) return;
            var stamp = Stamp(Path);
            if (stamp is null || (_stamp is { } known && known == stamp.Value)) return;
            _changed();
        }

        public void Dispose() => _timer.Dispose();
    }
}
