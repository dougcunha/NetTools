using NetTools.Helpers;
using System.Diagnostics.CodeAnalysis;

namespace NetTools.Tests.Helpers;

[ExcludeFromCodeCoverage]
public sealed class NugetVersionComparerTests
{
    [Theory]
    [InlineData("1.0.0", "2.0.0", "2.0.0")]
    [InlineData("2.0.0", "1.0.0", "2.0.0")]
    [InlineData("1.0.0", "1.0.0", "1.0.0")]
    [InlineData("1.0.0", "1.0.1", "1.0.1")]
    [InlineData("1.0.1", "1.0.0", "1.0.1")]
    [InlineData("1.0.0-alpha", "1.0.0", "1.0.0")]
    [InlineData("1.0.0", "1.0.0-alpha", "1.0.0")]
    [InlineData("1.0.0-alpha", "1.0.0-beta", "1.0.0-beta")]
    [InlineData("1.0.0-beta", "1.0.0-alpha", "1.0.0-beta")]
    [InlineData("2.0.0-alpha", "1.0.0", "2.0.0-alpha")]
    [InlineData("1.0.0", "2.0.0-alpha", "2.0.0-alpha")]
    public void GetGreaterVersion_CompareVersions_ReturnsGreaterVersion(string version1, string version2, string expected)
    {
        // Act
        var result = NugetVersionComparer.GetGreaterVersion(version1, version2);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("1.0.0-alpha", true)]
    [InlineData("1.0.0-beta", true)]
    [InlineData("1.0.0-beta.1", true)]
    [InlineData("1.0.0-rc", true)]
    [InlineData("1.0.0-preview", true)]
    [InlineData("2.0.0-alpha.1", true)]
    [InlineData("1.0.0", false)]
    [InlineData("2.0.0", false)]
    [InlineData("1.5.3", false)]
    [InlineData("10.0.0", false)]
    public void IsPrerelease_VariousVersions_ReturnsCorrectResult(string version, bool expected)
    {
        // Act
        var result = NugetVersionComparer.IsPrerelease(version);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void GetGreaterVersion_SemVerCompliance_HandlesComplexVersions()
    {
        // Arrange
        const string VERSION1 = "1.0.0-alpha.1+build.1";
        const string VERSION2 = "1.0.0-alpha.2+build.2";

        // Act
        var result = NugetVersionComparer.GetGreaterVersion(VERSION1, VERSION2);

        // Assert
        result.ShouldBe(VERSION2);
    }

    [Fact]
    public void GetGreaterVersion_MajorVersionDifference_ReturnsHigherMajor()
    {
        // Arrange
        const string VERSION1 = "1.9.9";
        const string VERSION2 = "2.0.0";

        // Act
        var result = NugetVersionComparer.GetGreaterVersion(VERSION1, VERSION2);

        // Assert
        result.ShouldBe(VERSION2);
    }

    [Fact]
    public void GetGreaterVersion_MinorVersionDifference_ReturnsHigherMinor()
    {
        // Arrange
        const string VERSION1 = "1.1.9";
        const string VERSION2 = "1.2.0";

        // Act
        var result = NugetVersionComparer.GetGreaterVersion(VERSION1, VERSION2);

        // Assert
        result.ShouldBe(VERSION2);
    }

    [Fact]
    public void GetGreaterVersion_PatchVersionDifference_ReturnsHigherPatch()
    {
        // Arrange
        const string VERSION1 = "1.0.1";
        const string VERSION2 = "1.0.2";

        // Act
        var result = NugetVersionComparer.GetGreaterVersion(VERSION1, VERSION2);

        // Assert
        result.ShouldBe(VERSION2);
    }

    [Fact]
    public void GetGreaterVersion_PrereleaseVsStable_ReturnsStable()
    {
        // Arrange
        const string PRERELEASE_VERSION = "1.0.0-alpha";
        const string STABLE_VERSION = "1.0.0";

        // Act
        var result = NugetVersionComparer.GetGreaterVersion(PRERELEASE_VERSION, STABLE_VERSION);

        // Assert
        result.ShouldBe(STABLE_VERSION);
    }

    [Fact]
    public void GetGreaterVersion_HigherPrereleaseVsLowerStable_ReturnsHigherPrerelease()
    {
        // Arrange
        const string PRERELEASE_VERSION = "2.0.0-alpha";
        const string STABLE_VERSION = "1.0.0";

        // Act
        var result = NugetVersionComparer.GetGreaterVersion(PRERELEASE_VERSION, STABLE_VERSION);

        // Assert
        result.ShouldBe(PRERELEASE_VERSION);
    }

    [Fact]
    public void GetGreaterVersion_MultiplePrereleaseIdentifiers_ReturnsCorrectVersion()
    {
        // Arrange
        const string VERSION1 = "1.0.0-alpha.1";
        const string VERSION2 = "1.0.0-alpha.2";

        // Act
        var result = NugetVersionComparer.GetGreaterVersion(VERSION1, VERSION2);

        // Assert
        result.ShouldBe(VERSION2);
    }

    [Fact]
    public void GetGreaterVersion_DifferentPrereleaseTypes_ReturnsCorrectVersion()
    {
        // Arrange
        const string ALPHA_VERSION = "1.0.0-alpha";
        const string BETA_VERSION = "1.0.0-beta";

        // Act
        var result = NugetVersionComparer.GetGreaterVersion(ALPHA_VERSION, BETA_VERSION);

        // Assert
        result.ShouldBe(BETA_VERSION);
    }

    [Fact]
    public void IsPrerelease_ReleaseCandidate_ReturnsTrue()
    {
        // Arrange
        const string RC_VERSION = "1.0.0-rc.1";

        // Act
        var result = NugetVersionComparer.IsPrerelease(RC_VERSION);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsPrerelease_PreviewVersion_ReturnsTrue()
    {
        // Arrange
        const string PREVIEW_VERSION = "1.0.0-preview.1";

        // Act
        var result = NugetVersionComparer.IsPrerelease(PREVIEW_VERSION);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsPrerelease_StableVersion_ReturnsFalse()
    {
        // Arrange
        const string STABLE_VERSION = "1.0.0";

        // Act
        var result = NugetVersionComparer.IsPrerelease(STABLE_VERSION);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsPrerelease_VersionWithBuildMetadata_ReturnsCorrectResult()
    {
        // Arrange
        const string VERSION_WITH_METADATA = "1.0.0+build.1";

        // Act
        var result = NugetVersionComparer.IsPrerelease(VERSION_WITH_METADATA);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsPrerelease_PrereleaseWithBuildMetadata_ReturnsTrue()
    {
        // Arrange
        const string PRERELEASE_WITH_METADATA = "1.0.0-alpha+build.1";

        // Act
        var result = NugetVersionComparer.IsPrerelease(PRERELEASE_WITH_METADATA);

        // Assert
        result.ShouldBeTrue();
    }
}
