using System.IO;
using Velopack;
using Velopack.Logging;
using Velopack.Sources;

namespace DBADashGUI.Viewers
{
    /// <summary>
    /// The installer feed for exactly one version of the GUI - the version of the repository database it connects to - rather
    /// than the latest.  The GUI has to match the repository, so a newer GUI is no better than an older one.
    ///
    /// Each release publishes its own copy of the feed alongside its packages, so the feed in the release for the
    /// repository's version knows about that version and the ones before it, and nothing after.  It's filtered to that
    /// version all the same, so a feed with later releases in it (a test feed)
    /// can't offer one.  Deltas up to the version are kept: a delta from a release before the previous one is in that
    /// release, not this one, and Velopack falls back to the full package when it can't download it.
    /// </summary>
    public sealed class RepositoryVersionSource : IUpdateSource
    {
        private readonly IUpdateSource _feed;

        /// <summary>The version to install - the repository's, to three parts.</summary>
        public Version Target { get; }

        /// <summary>Where the feed is read from: the folder or URL.  For the log, and a test.</summary>
        public string FeedLocation { get; }

        public RepositoryVersionSource(IUpdateSource feed, Version target, string feedLocation = null)
        {
            _feed = feed;
            Target = target;
            FeedLocation = feedLocation;
        }

        /// <summary>
        /// The feed published with the release for <paramref name="target"/>, or the folder or URL in
        /// <paramref name="feedOverride"/>.
        /// </summary>
        public static RepositoryVersionSource For(string repository, Version target, string feedOverride = null)
        {
            target = new Version(target.Major, target.Minor, Math.Max(target.Build, 0));
            var location = string.IsNullOrWhiteSpace(feedOverride)
                ? $"{repository}/releases/download/{target}/"
                : feedOverride;

            IUpdateSource feed = Directory.Exists(location)
                ? new SimpleFileSource(new DirectoryInfo(location))
                : new SimpleWebSource(location);

            return new RepositoryVersionSource(feed, target, location);
        }

        public async Task<VelopackAssetFeed> GetReleaseFeed(IVelopackLogger logger, string appId, string channel,
            Guid? stagingId = null, VelopackAsset latestLocalRelease = null)
        {
            var feed = await _feed.GetReleaseFeed(logger, appId, channel, stagingId, latestLocalRelease).ConfigureAwait(false);
            return new VelopackAssetFeed { Assets = Filter(feed.Assets, Target) };
        }

        public Task DownloadReleaseEntry(IVelopackLogger logger, VelopackAsset releaseEntry, string localFile, Action<int> progress,
            CancellationToken cancelToken = default) =>
            _feed.DownloadReleaseEntry(logger, releaseEntry, localFile, progress, cancelToken);

        /// <summary>
        /// What of a feed can take the GUI to <paramref name="target"/>: its full package and the deltas up to it.  Nothing at
        /// all if there is no full package for that version - otherwise Velopack would offer the latest one before it.
        /// </summary>
        public static VelopackAsset[] Filter(VelopackAsset[] assets, Version target)
        {
            var matches = assets.Where(a => IsTarget(a.Version, target)).ToList();
            if (!matches.Any(a => a.Type == VelopackAssetType.Full)) return [];

            return assets.Where(a => a.Type == VelopackAssetType.Delta && IsBefore(a.Version, target))
                .Concat(matches)
                .ToArray();
        }

        private static bool IsTarget(SemanticVersion version, Version target) =>
            version.Major == target.Major && version.Minor == target.Minor && version.Patch == Math.Max(target.Build, 0) &&
            !version.IsPrerelease;

        private static bool IsBefore(SemanticVersion version, Version target) =>
            new Version(version.Major, version.Minor, version.Patch) < new Version(target.Major, target.Minor, Math.Max(target.Build, 0));
    }
}
