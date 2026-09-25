using DBADashGUI.Viewers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http;
using System.Text;

namespace DBADash.Viewers.GUI.Test
{
    /// <summary>
    /// The update check: what counts as a release of the app, and when the app looks for one.
    /// </summary>
    [TestClass]
    public class UpdateTests
    {
        private const string Prefix = "DBADash_Visualizer_";

        private sealed class FakeHandler : HttpMessageHandler
        {
            public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
            public string Body { get; set; } = "{}";
            public HttpRequestMessage LastRequest { get; private set; }
            public int Requests { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests++;
                LastRequest = request;
                return Task.FromResult(new HttpResponseMessage(Status)
                {
                    Content = new StringContent(Body, Encoding.UTF8, "application/json")
                });
            }
        }

        private sealed class DictionaryStore : IViewerSettingsStore
        {
            public Dictionary<string, object> Values { get; } = new();
            public object Get(string name) => Values.GetValueOrDefault(name);
            public void Set(string name, object value) => Values[name] = value;

            public void Save()
            {
            }
        }

        private static string Release(string tag, bool draft = false, bool prerelease = false, params string[] assets)
        {
            var list = string.Join(",", assets.Select(a =>
                $"{{\"name\":\"{a}\",\"browser_download_url\":\"https://github.com/trimble-oss/dba-dash/releases/download/{tag}/{a}\"}}"));
            return $"{{\"tag_name\":\"{tag}\",\"draft\":{draft.ToString().ToLowerInvariant()}," +
                   $"\"prerelease\":{prerelease.ToString().ToLowerInvariant()}," +
                   $"\"html_url\":\"https://github.com/trimble-oss/dba-dash/releases/tag/{tag}\",\"assets\":[{list}]}}";
        }

        private static ReleaseChecker Checker(FakeHandler handler) =>
            new(new HttpClient(handler), "trimble-oss", "dba-dash", Prefix);

        private static async Task<ReleaseInfo> Latest(string body)
        {
            var handler = new FakeHandler { Body = body };
            return await Checker(handler).GetLatestAsync();
        }

        [TestMethod]
        public async Task ALatestReleaseWithTheAppsZipIsFound()
        {
            var release = await Latest(Release("4.20.0", assets: ["DBADash_4.20.0.zip", Prefix + "4.20.0.zip"]));

            Assert.IsNotNull(release);
            Assert.AreEqual(new Version(4, 20, 0), release.Version);
            Assert.AreEqual("https://github.com/trimble-oss/dba-dash/releases/tag/4.20.0", release.ReleaseUrl);
            Assert.AreEqual($"https://github.com/trimble-oss/dba-dash/releases/download/4.20.0/{Prefix}4.20.0.zip", release.DownloadUrl);
        }

        [TestMethod]
        public async Task ATagWithAVIsRead()
        {
            var release = await Latest(Release("v4.20.1", assets: [Prefix + "4.20.1.zip"]));

            Assert.AreEqual(new Version(4, 20, 1), release?.Version);
        }

        [TestMethod]
        public async Task AReleaseWithoutTheAppsZipIsNotOneForTheApp()
        {
            // An older release from before the app existed.
            Assert.IsNull(await Latest(Release("4.18.0", assets: ["DBADash_4.18.0.zip", "DBADash_GUI_Only_4.18.0.zip"])));
        }

        [TestMethod]
        public async Task OnlyTheUnsignedZipIsNotOffered()
        {
            Assert.IsNull(await Latest(Release("4.20.0", assets: [Prefix + "4.20.0-unsigned.zip"])));
        }

        [TestMethod]
        public async Task ADraftOrPreReleaseIsNotOffered()
        {
            Assert.IsNull(await Latest(Release("4.20.0", draft: true, assets: [Prefix + "4.20.0.zip"])));
            Assert.IsNull(await Latest(Release("4.20.0", prerelease: true, assets: [Prefix + "4.20.0.zip"])));
        }

        [TestMethod]
        public async Task ATagThatIsNotAVersionIsIgnored()
        {
            Assert.IsNull(await Latest(Release("nightly", assets: [Prefix + "1.0.0.zip"])));
        }

        [TestMethod]
        public async Task JsonThatIsNotAReleaseIsIgnored()
        {
            Assert.IsNull(await Latest("[]"));
            Assert.IsNull(await Latest("{}"));
        }

        [TestMethod]
        public async Task GitHubIsToldWhoIsAsking()
        {
            var handler = new FakeHandler { Body = Release("4.20.0", assets: [Prefix + "4.20.0.zip"]) };
            await Checker(handler).GetLatestAsync();

            Assert.AreEqual("https://api.github.com/repos/trimble-oss/dba-dash/releases/latest", handler.LastRequest.RequestUri?.ToString());
            Assert.IsTrue(handler.LastRequest.Headers.UserAgent.Any(), "GitHub refuses a request with no user agent.");
        }

        [TestMethod]
        public async Task AnErrorFromGitHubThrows()
        {
            var handler = new FakeHandler { Status = HttpStatusCode.Forbidden };

            await Assert.ThrowsExactlyAsync<HttpRequestException>(() => Checker(handler).GetLatestAsync());
        }

        // ---- UpdateService

        private static readonly ReleaseInfo Newer = new(new Version(4, 20, 0), "https://example/release", "https://example/zip");
        private static readonly DateTime Now = new(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);

        private static UpdateService Service(DictionaryStore store, Func<Task<ReleaseInfo>> latest, Version current = null, DateTime? now = null) =>
            new(store, _ => latest(), current ?? new Version(4, 19, 0), () => now ?? Now);

        [TestMethod]
        public async Task ANewerReleaseIsOffered()
        {
            var store = new DictionaryStore();

            var offered = await Service(store, () => Task.FromResult(Newer)).CheckIfDueAsync();

            Assert.AreSame(Newer, offered);
            Assert.IsTrue(store.Values.ContainsKey(UpdateService.LastCheckKey), "The check should be recorded.");
        }

        [TestMethod]
        public async Task TheSameOrAnOlderReleaseIsNotOffered()
        {
            Assert.IsNull(await Service(new DictionaryStore(), () => Task.FromResult(Newer), new Version(4, 20, 0)).CheckIfDueAsync());
            Assert.IsNull(await Service(new DictionaryStore(), () => Task.FromResult(Newer), new Version(5, 0, 0)).CheckIfDueAsync());
            Assert.IsNull(await Service(new DictionaryStore(), () => Task.FromResult<ReleaseInfo>(null)).CheckIfDueAsync());
        }

        [TestMethod]
        public async Task SwitchedOffMeansNoCheck()
        {
            var calls = 0;
            var service = Service(new DictionaryStore(), () => { calls++; return Task.FromResult(Newer); });
            service.AutoCheck = false;

            Assert.IsNull(await service.CheckIfDueAsync());
            Assert.AreEqual(0, calls);
        }

        [TestMethod]
        public void CheckingIsOnUntilTurnedOff()
        {
            var store = new DictionaryStore();
            var service = Service(store, () => Task.FromResult(Newer));

            Assert.IsTrue(service.AutoCheck);
            service.AutoCheck = false;
            Assert.IsFalse(service.AutoCheck);
            service.AutoCheck = true;
            Assert.IsTrue(service.AutoCheck);
        }

        [TestMethod]
        public async Task ARecentCheckIsNotRepeatedButADayOldOneIs()
        {
            var calls = 0;
            var store = new DictionaryStore();
            Task<ReleaseInfo> Latest() { calls++; return Task.FromResult(Newer); }

            await Service(store, Latest, now: Now).CheckIfDueAsync();
            await Service(store, Latest, now: Now.AddHours(23)).CheckIfDueAsync();
            Assert.AreEqual(1, calls, "23 hours later is too soon.");

            await Service(store, Latest, now: Now.AddHours(25)).CheckIfDueAsync();
            Assert.AreEqual(2, calls);
        }

        [TestMethod]
        public async Task AClockThatWasWrongDoesNotStopChecking()
        {
            var calls = 0;
            var store = new DictionaryStore { Values = { [UpdateService.LastCheckKey] = Now.AddYears(1).ToString("o") } };

            await Service(store, () => { calls++; return Task.FromResult(Newer); }, now: Now).CheckIfDueAsync();

            Assert.AreEqual(1, calls);
        }

        [TestMethod]
        public async Task ASkippedVersionIsNotOfferedAtStartUpButIsWhenAsked()
        {
            var store = new DictionaryStore();
            var service = Service(store, () => Task.FromResult(Newer));
            service.Skip(Newer);

            Assert.IsNull(await service.CheckIfDueAsync());
            Assert.AreSame(Newer, await service.CheckNowAsync());
        }

        [TestMethod]
        public async Task AVersionAfterTheSkippedOneIsOffered()
        {
            var store = new DictionaryStore();
            Service(store, () => Task.FromResult(Newer)).Skip(Newer);
            var later = new ReleaseInfo(new Version(4, 21, 0), "https://example/release", "https://example/zip");

            Assert.AreSame(later, await Service(store, () => Task.FromResult(later)).CheckIfDueAsync());
        }

        [TestMethod]
        public async Task AFailedCheckAtStartUpIsSilentAndRetriedNextTime()
        {
            var store = new DictionaryStore();

            var offered = await Service(store, () => throw new HttpRequestException("offline")).CheckIfDueAsync();

            Assert.IsNull(offered);
            Assert.IsFalse(store.Values.ContainsKey(UpdateService.LastCheckKey), "A failure shouldn't count as a check.");
        }

        [TestMethod]
        public async Task AFailedCheckTheUserAskedForThrows()
        {
            var service = Service(new DictionaryStore(), () => throw new HttpRequestException("offline"));

            await Assert.ThrowsExactlyAsync<HttpRequestException>(() => service.CheckNowAsync());
        }
    }
}
