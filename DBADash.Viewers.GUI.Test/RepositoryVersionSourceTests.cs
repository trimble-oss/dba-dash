using DBADashGUI.Viewers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Velopack;
using Velopack.Logging;
using Velopack.Sources;

namespace DBADash.Viewers.GUI.Test
{
    /// <summary>
    /// The installer feed for an installed GUI: only ever the repository's version, never the latest.
    /// </summary>
    [TestClass]
    public class RepositoryVersionSourceTests
    {
        private static VelopackAsset Asset(string version, VelopackAssetType type = VelopackAssetType.Full) => new()
        {
            PackageId = "DBADash_GUI",
            Version = SemanticVersion.Parse(version),
            Type = type,
            FileName = $"DBADash_GUI-{version}-{(type == VelopackAssetType.Full ? "full" : "delta")}.nupkg"
        };

        private static readonly VelopackAsset[] Feed =
        [
            Asset("4.17.0"),
            Asset("4.18.0"), Asset("4.18.0", VelopackAssetType.Delta),
            Asset("4.19.0"), Asset("4.19.0", VelopackAssetType.Delta),
            Asset("4.20.0"), Asset("4.20.0", VelopackAssetType.Delta)
        ];

        private static string[] Names(IEnumerable<VelopackAsset> assets) => assets.Select(a => a.FileName).Order().ToArray();

        [TestMethod]
        public void KeepsTheTargetAndTheDeltasUpToIt()
        {
            var result = RepositoryVersionSource.Filter(Feed, new Version(4, 19, 0));

            CollectionAssert.AreEqual(new[]
            {
                "DBADash_GUI-4.18.0-delta.nupkg", "DBADash_GUI-4.19.0-delta.nupkg", "DBADash_GUI-4.19.0-full.nupkg"
            }, Names(result));
        }

        [TestMethod]
        public void NothingWhenTheTargetIsMissing()
        {
            // Otherwise Velopack would offer 4.18.0, the latest full package left - neither the version wanted nor the one running.
            var feed = Feed.Where(a => a.Version != SemanticVersion.Parse("4.19.0")).ToArray();

            Assert.AreEqual(0, RepositoryVersionSource.Filter(feed, new Version(4, 19, 0)).Length);
        }

        [TestMethod]
        public void PrereleaseIsNotTheTarget()
        {
            var feed = new[] { Asset("4.18.0"), Asset("4.19.0-beta") };

            Assert.AreEqual(0, RepositoryVersionSource.Filter(feed, new Version(4, 19, 0)).Length);
        }

        [TestMethod]
        public void AnOlderTargetForADowngrade()
        {
            var result = RepositoryVersionSource.Filter(Feed, new Version(4, 17, 0));

            CollectionAssert.AreEqual(new[] { "DBADash_GUI-4.17.0-full.nupkg" }, Names(result));
        }

        [TestMethod]
        public void TwoPartRepositoryVersionMeansPatchZero()
        {
            var result = RepositoryVersionSource.Filter(Feed, new Version(4, 19));

            CollectionAssert.Contains(Names(result), "DBADash_GUI-4.19.0-full.nupkg");
        }

        private sealed class FakeSource : IUpdateSource
        {
            public Task<VelopackAssetFeed> GetReleaseFeed(IVelopackLogger logger, string appId, string channel, Guid? stagingId = null,
                VelopackAsset latestLocalRelease = null) => Task.FromResult(new VelopackAssetFeed { Assets = Feed });

            public Task DownloadReleaseEntry(IVelopackLogger logger, VelopackAsset releaseEntry, string localFile, Action<int> progress,
                CancellationToken cancelToken = default) => Task.CompletedTask;
        }

        [TestMethod]
        public async Task FeedIsFiltered()
        {
            var source = new RepositoryVersionSource(new FakeSource(), new Version(4, 18, 0));

            var feed = await source.GetReleaseFeed(null, "DBADash_GUI", "gui");

            CollectionAssert.AreEqual(new[] { "DBADash_GUI-4.18.0-delta.nupkg", "DBADash_GUI-4.18.0-full.nupkg" }, Names(feed.Assets));
        }

        [TestMethod]
        public void ReadsTheFeedOfTheRepositoryVersionsRelease()
        {
            var source = RepositoryVersionSource.For("https://github.com/trimble-oss/dba-dash", new Version(4, 19, 0, 0));

            Assert.AreEqual("https://github.com/trimble-oss/dba-dash/releases/download/4.19.0/", source.FeedLocation);
        }
    }
}
