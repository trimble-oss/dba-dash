using System.Globalization;

namespace DBADashGUI.Viewers
{
    /// <summary>
    /// When to look for an update, and what to do with the answer: no more than once a day, not at all if the user has
    /// switched it off, and not again for a version the user has said they don't want yet.
    ///
    /// Its settings - whether to check, when it last did, which version was skipped - are kept in the same store as the
    /// viewers' own.
    /// </summary>
    public sealed class UpdateService
    {
        public const string CheckForUpdatesKey = "CheckForUpdates";
        public const string LastCheckKey = "LastUpdateCheckUtc";
        public const string SkippedVersionKey = "SkippedUpdateVersion";

        /// <summary>How long after a check before another is made automatically.</summary>
        public static readonly TimeSpan CheckInterval = TimeSpan.FromDays(1);

        private readonly IViewerSettingsStore _store;
        private readonly Func<CancellationToken, Task<ReleaseInfo>> _getLatest;
        private readonly Version _current;
        private readonly Func<DateTime> _utcNow;

        /// <param name="store">Where the update settings are kept.</param>
        /// <param name="getLatest">Finds the latest release - see <see cref="ReleaseChecker.GetLatestAsync"/>.</param>
        /// <param name="current">The version running.</param>
        /// <param name="utcNow">The time, for a test to control.</param>
        public UpdateService(IViewerSettingsStore store, Func<CancellationToken, Task<ReleaseInfo>> getLatest, Version current,
            Func<DateTime> utcNow = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _getLatest = getLatest ?? throw new ArgumentNullException(nameof(getLatest));
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        /// <summary>Whether to look for an update at start-up.  On unless the user turns it off.</summary>
        public bool AutoCheck
        {
            get => _store.Get(CheckForUpdatesKey) is not false;
            set
            {
                _store.Set(CheckForUpdatesKey, value);
                _store.Save();
            }
        }

        /// <summary>
        /// The update to offer at start-up, or null - because it is switched off, was checked recently, is already the
        /// version the user skipped, or there isn't one.  Never throws: a failed check is not worth telling anyone about,
        /// and is tried again next time.
        /// </summary>
        public async Task<ReleaseInfo> CheckIfDueAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!AutoCheck || !IsDue()) return null;

                var release = await _getLatest(cancellationToken);

                // Only a check that got an answer counts, so a failure isn't retried for a day.
                RecordCheck();

                return release != null && release.Version > _current && !IsSkipped(release) ? release : null;
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                return null;
            }
        }

        /// <summary>
        /// Looks now, at the user's request.  Null when this is the latest version.  A version the user skipped is still reported: they asked.
        /// Throws when GitHub can't be reached, as the user asked and should be told.
        /// </summary>
        public async Task<ReleaseInfo> CheckNowAsync(CancellationToken cancellationToken = default)
        {
            var release = await _getLatest(cancellationToken);
            RecordCheck();
            return release != null && release.Version > _current ? release : null;
        }

        /// <summary>Don't offer <paramref name="release"/> again at start-up.  A later version is offered.</summary>
        public void Skip(ReleaseInfo release)
        {
            _store.Set(SkippedVersionKey, release.Version.ToString());
            _store.Save();
        }

        private bool IsDue()
        {
            if (_store.Get(LastCheckKey) is not string text ||
                !DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var last))
            {
                return true;
            }

            // A time in the future is a clock that was wrong when it was written, not a reason to wait.
            var age = _utcNow() - last.ToUniversalTime();
            return age >= CheckInterval || age < TimeSpan.Zero;
        }

        private void RecordCheck()
        {
            _store.Set(LastCheckKey, _utcNow().ToString("o", CultureInfo.InvariantCulture));
            _store.Save();
        }

        private bool IsSkipped(ReleaseInfo release) =>
            _store.Get(SkippedVersionKey) is string skipped &&
            Version.TryParse(skipped, out var version) &&
            version == release.Version;
    }
}
