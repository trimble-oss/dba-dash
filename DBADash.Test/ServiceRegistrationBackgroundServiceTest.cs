using DBADashAI.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace DBADashConfig.Test;

[TestClass]
public class ServiceRegistrationBackgroundServiceTest
{
    [TestMethod]
    [DataRow("http://lab2022", 5055, "http://lab2022:5055")]
    [DataRow("http://lab2022", 8080, "http://lab2022:8080")]
    [DataRow("http://lab2022:9000", 5055, "http://lab2022:9000")]
    [DataRow("https://server.example.com", 5055, "https://server.example.com:5055")]
    [DataRow("https://server.example.com:443", 5055, "https://server.example.com:443")]
    [DataRow("http://localhost", null, "http://localhost:5055")]
    [DataRow("http://192.168.1.100", 7000, "http://192.168.1.100:7000")]
    [DataRow("http://server:8080/", 5055, "http://server:8080")]
    [DataRow("not a valid url", 5055, "not a valid url")]
    public void TestNormalizeServiceUrl(string inputUrl, int? configPort, string expectedUrl)
    {
        var port = configPort ?? 5055;
        string result;

        try
        {
            result = ServiceRegistrationBackgroundService.NormalizeServiceUrl(inputUrl, port);
        }
        catch (UriFormatException)
        {
            result = inputUrl;
        }

        Assert.AreEqual(expectedUrl, result);
    }

    [TestMethod]
    [DataRow("http://lab2022", 5055, true, "https://lab2022:5055")]
    [DataRow("https://lab2022:5055", 5055, false, "http://lab2022:5055")]
    [DataRow("https://server.example.com:443", 5055, true, "https://server.example.com:443")]
    [DataRow("http://server.example.com:8080", 5055, true, "https://server.example.com:8080")]
    public void TestNormalizeServiceUrl_RewritesScheme(string inputUrl, int port, bool useHttps, string expectedUrl)
    {
        var result = ServiceRegistrationBackgroundService.NormalizeServiceUrl(inputUrl, port, useHttps);
        Assert.AreEqual(expectedUrl, result);
    }
}
