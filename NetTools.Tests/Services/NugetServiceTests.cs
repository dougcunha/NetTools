using NetTools.Services;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;

namespace NetTools.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class NugetServiceTests
{
    private readonly NugetService _service;

    public NugetServiceTests()
    {
        var handler = Substitute.For<HttpMessageHandler>();
        var httpClient = new HttpClient(handler);
        _service = new NugetService(httpClient);
    }

    [Fact]
    public async Task GetLatestVersionAsync_NullPackageId_ReturnsNull()
    {
        // Act
        var result = await _service.GetLatestVersionAsync(null!);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetLatestVersionAsync_EmptyPackageId_ReturnsNull()
    {
        // Act
        var result = await _service.GetLatestVersionAsync("");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetLatestVersionAsync_WhitespacePackageId_ReturnsNull()
    {
        // Act
        var result = await _service.GetLatestVersionAsync("   ");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetLatestVersionAsync_HttpRequestFails_ReturnsNull()
    {
        // Arrange
        const string PACKAGE_ID = "TestPackage";
        using var httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.NotFound));
        var service = new NugetService(httpClient);

        // Act
        var result = await service.GetLatestVersionAsync(PACKAGE_ID);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetLatestVersionAsync_ValidPackageWithoutPrerelease_ReturnsLatestStableVersion()
    {
        // Arrange
        const string PACKAGE_ID = "TestPackage";

        const string JSON_RESPONSE = """
            {
                "versions": [
                    "1.0.0",
                    "1.1.0",
                    "2.0.0-beta",
                    "1.2.0",
                    "2.0.0-rc"
                ]
            }
            """;

        using var httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK, JSON_RESPONSE));
        var service = new NugetService(httpClient);

        // Act
        var result = await service.GetLatestVersionAsync(PACKAGE_ID, includePrerelease: false);

        // Assert
        result.ShouldBe("1.2.0");
    }

    [Fact]
    public async Task GetLatestVersionAsync_ValidPackageWithPrerelease_ReturnsLatestVersion()
    {
        // Arrange
        const string PACKAGE_ID = "TestPackage";

        const string JSON_RESPONSE = """
            {
                "versions": [
                    "1.0.0",
                    "1.1.0",
                    "2.0.0-beta",
                    "1.2.0",
                    "2.0.0-rc"
                ]
            }
            """;

        using var httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK, JSON_RESPONSE));
        var service = new NugetService(httpClient);

        // Act
        var result = await service.GetLatestVersionAsync(PACKAGE_ID, includePrerelease: true);

        // Assert
        result.ShouldBe("2.0.0-rc");
    }

    [Fact]
    public async Task GetLatestVersionAsync_EmptyVersionsArray_ReturnsNull()
    {
        // Arrange
        const string PACKAGE_ID = "TestPackage";

        const string JSON_RESPONSE = """
            {
                "versions": []
            }
            """;

        using var httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK, JSON_RESPONSE));
        var service = new NugetService(httpClient);

        // Act
        var result = await service.GetLatestVersionAsync(PACKAGE_ID);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetLatestVersionAsync_MissingVersionsProperty_ReturnsNull()
    {
        // Arrange
        const string PACKAGE_ID = "TestPackage";

        const string JSON_RESPONSE = """
            {
                "otherProperty": "value"
            }
            """;

        using var httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK, JSON_RESPONSE));
        var service = new NugetService(httpClient);

        // Act
        var result = await service.GetLatestVersionAsync(PACKAGE_ID);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetLatestVersionAsync_InvalidJson_ReturnsNull()
    {
        // Arrange
        const string PACKAGE_ID = "TestPackage";
        const string INVALID_JSON = "{ invalid json }";

        using var httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK, INVALID_JSON));
        var service = new NugetService(httpClient);

        // Act
        var result = await service.GetLatestVersionAsync(PACKAGE_ID);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetLatestVersionAsync_OnlyPrereleaseVersionsWithoutPrereleaseFlag_ReturnsNull()
    {
        // Arrange
        const string PACKAGE_ID = "TestPackage";

        const string JSON_RESPONSE = """
            {
                "versions": [
                    "1.0.0-alpha",
                    "1.0.0-beta",
                    "2.0.0-rc"
                ]
            }
            """;

        using var httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK, JSON_RESPONSE));
        var service = new NugetService(httpClient);

        // Act
        var result = await service.GetLatestVersionAsync(PACKAGE_ID, includePrerelease: false);

        // Assert
        result.ShouldBeNull();
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string? _responseContent;

        public MockHttpMessageHandler(HttpStatusCode statusCode, string? responseContent = null)
        {
            _statusCode = statusCode;
            _responseContent = responseContent;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode);

            if (_responseContent != null)
                response.Content = new StringContent(_responseContent, Encoding.UTF8, "application/json");

            return Task.FromResult(response);
        }
    }
}
