using System.Text.Json;

namespace NetTools.Services;

/// <summary>
/// Service for interacting with the NuGet API.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NugetService"/> class with an injected <see cref="HttpClient"/>.
/// </remarks>
/// <param name="httpClient">The <see cref="HttpClient"/> instance to use.</param>
public sealed class NugetService(HttpClient httpClient) : INugetService
{
    #pragma warning disable S1075
    private const string NUGET_V3_URL = "https://api.nuget.org/v3-flatcontainer/";
    #pragma warning restore S1075

    /// <inheritdoc />
    public async Task<string?> GetLatestVersionAsync(string packageId, bool includePrerelease = false)
    {
        if (string.IsNullOrWhiteSpace(packageId))
            return null;

        var url = $"{NUGET_V3_URL}{packageId.ToLowerInvariant()}/index.json";

        try
        {
            using var response = await httpClient.GetAsync(url).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

            if (doc.RootElement.TryGetProperty("versions", out var versions) && versions.ValueKind == JsonValueKind.Array && versions.GetArrayLength() > 0)
            {
                var versionList =
                (
                    from v in versions.EnumerateArray()
                    select v.GetString()
                    into ver
                    where !string.IsNullOrEmpty(ver)
                    where includePrerelease || !ver.Contains('-')
                    select ver
                ).ToList();

                if (versionList.Count > 0)
                    return versionList[^1];
            }
        }
        catch
        {
            // Ignore errors and return null
        }

        return null;
    }
}
