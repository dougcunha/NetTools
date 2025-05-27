using System.Xml.Linq;
using System.IO.Abstractions;
using NetTools.Helpers;
using NetTools.Models;
using NetTools.Services;
using Spectre.Console.Testing;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace NetTools.Tests.Helpers;

[ExcludeFromCodeCoverage]
public sealed class CsprojHelpersTests
{
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IXmlService _xmlService = Substitute.For<IXmlService>();
    private readonly TestConsole _console = new();
    private readonly CsprojHelpers _helpers;

    public CsprojHelpersTests()
        => _helpers = new CsprojHelpers(_fileSystem, _xmlService, _console);

    [Fact]
    public void GetPackagesFromCsproj_FileDoesNotExist_ReturnsEmptyDictionary()
    {
        // Arrange
        const string PATH = "test.csproj";
        _fileSystem.File.Exists(PATH).Returns(false);

        // Act
        var result = _helpers.GetPackagesFromCsproj(PATH);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void GetPackagesFromCsproj_ValidCsproj_ReturnsPackageVersions()
    {
        // Arrange
        const string PATH = "project.csproj";
        _fileSystem.File.Exists(PATH).Returns(true);

        const string CSPROJ_CONTENT = """
            <Project>
              <ItemGroup>
                <PackageReference Include="PkgA" Version="1.2.3" />
                <PackageReference Include="PkgB" ><Version>2.0.0</Version></PackageReference>
              </ItemGroup>
            </Project>
            """;

        var xml = XDocument.Parse(CSPROJ_CONTENT);
        _xmlService.Load(PATH, Arg.Any<LoadOptions>()).Returns(xml);
        _xmlService.Load(PATH).Returns(xml);

        // Act
        var result = _helpers.GetPackagesFromCsproj(PATH);

        // Assert
        result.Count.ShouldBe(2);
        result["PkgA"].ShouldBe("1.2.3");
        result["PkgB"].ShouldBe("2.0.0");
    }

    [Fact]
    public void RetrieveUniquePackageVersions_MultipleProjects_ReturnsHighestVersions()
    {
        // Arrange
        var projectsPackages = new Dictionary<string, List<Package>>
        {
            ["proj1"] = [new Package("PkgX", "1.0.0"), new Package("PkgY", "2.0.0")],
            ["proj2"] = [new Package("PkgX", "1.1.0"), new Package("PkgZ", "3.0.0")]
        };

        // Act
        var result = _helpers.RetrieveUniquePackageVersions(projectsPackages);

        // Assert
        result.Count.ShouldBe(3);
        result["PkgX"].ShouldBe("1.1.0");
        result["PkgY"].ShouldBe("2.0.0");
        result["PkgZ"].ShouldBe("3.0.0");
    }

    [Fact]
    public void GetOutdatedPackages_ExcludePrereleaseInstalled_ReturnsOnlyStableOutdated()
    {
        // Arrange
        var allPackages = new Dictionary<string, string>
        {
            ["PkgStable"] = "1.0.0",
            ["PkgPre"] = "2.0.0-beta"
        };

        var latestVersions = new Dictionary<string, string?>
        {
            ["PkgStable"] = "1.1.0",
            ["PkgPre"] = "2.0.1"
        };

        // Act
        var result = _helpers.GetOutdatedPackages(allPackages, latestVersions, includePrerelease: false);

        // Assert
        result.Count.ShouldBe(1);
        result[0].PackageId.ShouldBe("PkgStable");
        result[0].Installed.ShouldBe("1.0.0");
        result[0].Latest.ShouldBe("1.1.0");
    }

    [Fact]
    public void GetOutdatedPackages_IncludePrereleaseInstalled_ReturnsAllOutdated()
    {
        // Arrange
        var allPackages = new Dictionary<string, string>
        {
            ["PkgStable"] = "1.0.0",
            ["PkgPre"] = "2.0.0-beta"
        };

        var latestVersions = new Dictionary<string, string?>
        {
            ["PkgStable"] = "1.1.0",
            ["PkgPre"] = "2.0.1"
        };

        // Act
        var result = _helpers.GetOutdatedPackages(allPackages, latestVersions, includePrerelease: true);

        // Assert
        result.Count.ShouldBe(2);
        var ids = result.ConvertAll(static r => r.PackageId);
        ids.ShouldContain("PkgStable");
        ids.ShouldContain("PkgPre");
    }

    [Fact]
    public void RemovePackageFromCsproj_PackageExists_RemovesAndWritesXml()
    {
        // Arrange
        const string PATH = "proj.csproj";

        const string XML_CONTENT = """
            <Project>
              <ItemGroup>
                <PackageReference Include="PkgRem" Version="1.0.0"/>
                <PackageReference Include="Other" Version="2.0.0"/>
              </ItemGroup>
            </Project>
            """;

        var doc = XDocument.Parse(XML_CONTENT);
        _xmlService.Load(PATH, LoadOptions.PreserveWhitespace).Returns(doc);

        // Act
        _helpers.RemovePackageFromCsproj(PATH, "PkgRem");

        // Assert
        _xmlService.Received(1)
            .WriteTo(PATH,
                Arg.Is<string>(static s => s.Contains("Other") && !s.Contains("PkgRem")),
                Arg.Any<XmlWriterSettings>());
    }

    [Fact]
    public void RemovePackageFromCsproj_PackageNotExists_DoesNotWriteXml()
    {
        // Arrange
        const string PATH = "proj.csproj";

        const string XML_CONTENT = """
            <Project>
              <ItemGroup>
                <PackageReference Include="PkgA" Version="1.0.0"/>
              </ItemGroup>
            </Project>
            """;

        var doc = XDocument.Parse(XML_CONTENT);
        _xmlService.Load(PATH, LoadOptions.PreserveWhitespace).Returns(doc);

        // Act
        _helpers.RemovePackageFromCsproj(PATH, "NonExist");

        // Assert
        _xmlService.DidNotReceive().WriteTo(PATH, Arg.Any<string>(), Arg.Any<XmlWriterSettings>());
    }

    [Fact]
    public void HasPackage_PackageExists_ReturnsTrue()
    {
        // Arrange
        const string PATH = "proj.csproj";

        const string XML_CONTENT = """
            <Project>
              <ItemGroup>
                <PackageReference Include="Exists" Version="1.0.0"/>
              </ItemGroup>
            </Project>
            """;

        var doc = XDocument.Parse(XML_CONTENT);
        _xmlService.Load(PATH, LoadOptions.PreserveWhitespace).Returns(doc);

        // Act
        var result = _helpers.HasPackage(PATH, "Exists");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void HasPackage_PackageNotExists_ReturnsFalse()
    {
        // Arrange
        const string PATH = "proj.csproj";

        const string XML_CONTENT = """
            <Project>
              <ItemGroup>
                <PackageReference Include="Other" Version="1.0.0"/>
              </ItemGroup>
            </Project>
            """;

        var doc = XDocument.Parse(XML_CONTENT);
        _xmlService.Load(PATH, LoadOptions.PreserveWhitespace).Returns(doc);

        // Act
        var result = _helpers.HasPackage(PATH, "Missing");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void UpdatePackageVersionInCsproj_AttributeVersion_UpdatesAndWritesXml()
    {
        // Arrange
        const string PATH = "proj.csproj";

        const string XML_CONTENT = """
            <Project>
              <ItemGroup>
                <PackageReference Include="Pkg1" Version="1.0.0"/>
              </ItemGroup>
            </Project>
            """;

        var doc = XDocument.Parse(XML_CONTENT);
        _xmlService.Load(PATH, LoadOptions.PreserveWhitespace).Returns(doc);

        // Act
        _helpers.UpdatePackageVersionInCsproj(PATH, "Pkg1", "2.0.0");

        // Assert
        _xmlService.Received(1)
            .WriteTo(PATH,
                Arg.Is<string>(s => s.Contains("Version=\"2.0.0\"")),
                Arg.Any<XmlWriterSettings>());
    }

    [Fact]
    public void UpdatePackageVersionInCsproj_NestedVersion_UpdatesAndWritesXml()
    {
        // Arrange
        const string PATH = "proj.csproj";

        const string XML_CONTENT = """
            <Project>
              <ItemGroup>
                <PackageReference Include="Pkg2"><Version>1.0.0</Version></PackageReference>
              </ItemGroup>
            </Project>
            """;

        var doc = XDocument.Parse(XML_CONTENT);
        _xmlService.Load(PATH, LoadOptions.PreserveWhitespace).Returns(doc);

        // Act
        _helpers.UpdatePackageVersionInCsproj(PATH, "Pkg2", "3.0.0");

        // Assert
        _xmlService.Received(1)
            .WriteTo(PATH,
                Arg.Is<string>(s => s.Contains("<Version>3.0.0</Version>")),
                Arg.Any<XmlWriterSettings>());
    }

    [Fact]
    public void UpdatePackageVersionInCsproj_NoVersionElementOrAttribute_DoesNotWriteXml()
    {
        // Arrange
        const string PATH = "proj.csproj";

        const string XML_CONTENT = """
            <Project>
              <ItemGroup>
                <PackageReference Include="Pkg3"/>
              </ItemGroup>
            </Project>
            """;

        var doc = XDocument.Parse(XML_CONTENT);
        _xmlService.Load(PATH, LoadOptions.PreserveWhitespace).Returns(doc);

        // Act
        _helpers.UpdatePackageVersionInCsproj(PATH, "Pkg3", "4.0.0");

        // Assert
        _xmlService.DidNotReceive().WriteTo(PATH, Arg.Any<string>(), Arg.Any<XmlWriterSettings>());
    }

    [Fact]
    public void UpdatePackagesInProjects_ValidSelectedPackages_UpdatesProjectsCorrectly()
    {
        // Arrange
        var projectPackages = new Dictionary<string, List<Package>>
        {
            ["project1.csproj"] = [new("PkgA", "1.0.0"), new("PkgB", "2.0.0")],
            ["project2.csproj"] = [new("PkgA", "1.0.0"), new("PkgC", "3.0.0")]
        };

        var latestVersions = new Dictionary<string, string?>
        {
            ["PkgA"] = "1.5.0",
            ["PkgB"] = "2.5.0",
            ["PkgC"] = "3.5.0"
        };

        var selected = new List<(string, string)>
        {
            ("Package A", "PkgA"),
            ("Package B", "PkgB")
        };

        const string XML_CONTENT_PROJ1 = """
            <Project>
              <ItemGroup>
                <PackageReference Include="PkgA" Version="1.0.0" />
                <PackageReference Include="PkgB" Version="2.0.0" />
              </ItemGroup>
            </Project>
            """;

        const string XML_CONTENT_PROJ2 = """
            <Project>
              <ItemGroup>
                <PackageReference Include="PkgA" Version="1.0.0" />
                <PackageReference Include="PkgC" Version="3.0.0" />
              </ItemGroup>
            </Project>
            """;

        var doc1 = XDocument.Parse(XML_CONTENT_PROJ1);
        var doc2 = XDocument.Parse(XML_CONTENT_PROJ2);

        _xmlService.Load("project1.csproj", LoadOptions.PreserveWhitespace).Returns(doc1);
        _xmlService.Load("project2.csproj", LoadOptions.PreserveWhitespace).Returns(doc2);

        // Act
        _helpers.UpdatePackagesInProjects(projectPackages, latestVersions, selected);

        // Assert
        _xmlService.Received(2).WriteTo("project1.csproj", Arg.Any<string>(), Arg.Any<XmlWriterSettings>());
        _xmlService.Received(1).WriteTo("project2.csproj", Arg.Any<string>(), Arg.Any<XmlWriterSettings>());
    }

    [Fact]
    public void UpdatePackagesInProjects_PackageNotInProject_SkipsProject()
    {
        // Arrange
        var projectPackages = new Dictionary<string, List<Package>>
        {
            ["project1.csproj"] = [new("PkgA", "1.0.0")]
        };

        var latestVersions = new Dictionary<string, string?>
        {
            ["PkgB"] = "2.0.0"
        };

        var selected = new List<(string, string)>
        {
            ("Package B", "PkgB")
        };

        // Act
        _helpers.UpdatePackagesInProjects(projectPackages, latestVersions, selected);

        // Assert
        _xmlService.DidNotReceive().WriteTo(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<XmlWriterSettings>());
    }    [Fact]
    public void UpdatePackagesInProjects_NoLatestVersionFound_LogsErrorAndContinues()
    {
        // Arrange
        var projectPackages = new Dictionary<string, List<Package>>
        {
            ["project1.csproj"] = [new("PkgA", "1.0.0")]
        };

        var latestVersions = new Dictionary<string, string?>
        {
            ["PkgA"] = null
        };

        var selected = new List<(string, string)>
        {
            ("Package A", "PkgA")
        };

        // Act
        _helpers.UpdatePackagesInProjects(projectPackages, latestVersions, selected);

        // Assert
        _console.Output.ShouldContain("No latest version found for package 'PkgA' in project 'project1.csproj'.");
        _xmlService.DidNotReceive().WriteTo(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<XmlWriterSettings>());
    }    [Fact]
    public void UpdatePackagesInProjects_EmptyLatestVersion_LogsErrorAndContinues()
    {
        // Arrange
        var projectPackages = new Dictionary<string, List<Package>>
        {
            ["project1.csproj"] = [new("PkgA", "1.0.0")]
        };

        var latestVersions = new Dictionary<string, string?>
        {
            ["PkgA"] = ""
        };

        var selected = new List<(string, string)>
        {
            ("Package A", "PkgA")
        };

        // Act
        _helpers.UpdatePackagesInProjects(projectPackages, latestVersions, selected);

        // Assert
        _console.Output.ShouldContain("No latest version found for package 'PkgA' in project 'project1.csproj'.");
        _xmlService.DidNotReceive().WriteTo(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<XmlWriterSettings>());
    }

    [Fact]
    public void GetPackagesFromProjects_MultipleProjects_ReturnsAllPackages()
    {
        // Arrange
        var projects = new List<string> { "project1.csproj", "project2.csproj" };

        _fileSystem.File.Exists("project1.csproj").Returns(true);
        _fileSystem.File.Exists("project2.csproj").Returns(true);

        const string CSPROJ_CONTENT_1 = """
            <Project>
              <ItemGroup>
                <PackageReference Include="PkgA" Version="1.0.0" />
                <PackageReference Include="PkgB" Version="2.0.0" />
              </ItemGroup>
            </Project>
            """;

        const string CSPROJ_CONTENT_2 = """
            <Project>
              <ItemGroup>
                <PackageReference Include="PkgC" Version="3.0.0" />
                <PackageReference Include="PkgD" Version="4.0.0" />
              </ItemGroup>
            </Project>
            """;

        var doc1 = XDocument.Parse(CSPROJ_CONTENT_1);
        var doc2 = XDocument.Parse(CSPROJ_CONTENT_2);

        _xmlService.Load("project1.csproj").Returns(doc1);
        _xmlService.Load("project2.csproj").Returns(doc2);

        // Act
        var result = _helpers.GetPackagesFromProjects(projects);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);

        result["project1.csproj"].ShouldNotBeNull();
        result["project1.csproj"].Count.ShouldBe(2);
        result["project1.csproj"].ShouldContain(p => p.Id == "PkgA" && p.Version == "1.0.0");
        result["project1.csproj"].ShouldContain(p => p.Id == "PkgB" && p.Version == "2.0.0");

        result["project2.csproj"].ShouldNotBeNull();
        result["project2.csproj"].Count.ShouldBe(2);
        result["project2.csproj"].ShouldContain(p => p.Id == "PkgC" && p.Version == "3.0.0");
        result["project2.csproj"].ShouldContain(p => p.Id == "PkgD" && p.Version == "4.0.0");
    }

    [Fact]
    public void GetPackagesFromProjects_ProjectWithNoPackages_ReturnsEmptyListForProject()
    {
        // Arrange
        var projects = new List<string> { "empty-project.csproj" };

        _fileSystem.File.Exists("empty-project.csproj").Returns(true);

        const string EMPTY_CSPROJ_CONTENT = """
            <Project>
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """;

        var doc = XDocument.Parse(EMPTY_CSPROJ_CONTENT);
        _xmlService.Load("empty-project.csproj").Returns(doc);

        // Act
        var result = _helpers.GetPackagesFromProjects(projects);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    [Fact]
    public void GetPackagesFromProjects_NonExistentProject_SkipsProject()
    {
        // Arrange
        var projects = new List<string> { "nonexistent.csproj", "valid-project.csproj" };

        _fileSystem.File.Exists("nonexistent.csproj").Returns(false);
        _fileSystem.File.Exists("valid-project.csproj").Returns(true);

        const string VALID_CSPROJ_CONTENT = """
            <Project>
              <ItemGroup>
                <PackageReference Include="PkgA" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """;

        var validDoc = XDocument.Parse(VALID_CSPROJ_CONTENT);
        _xmlService.Load("valid-project.csproj").Returns(validDoc);

        // Act
        var result = _helpers.GetPackagesFromProjects(projects);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result.ContainsKey("nonexistent.csproj").ShouldBeFalse();
        result.ContainsKey("valid-project.csproj").ShouldBeTrue();
        result["valid-project.csproj"].Count.ShouldBe(1);
        result["valid-project.csproj"][0].Id.ShouldBe("PkgA");
        result["valid-project.csproj"][0].Version.ShouldBe("1.0.0");
    }

    [Fact]
    public void GetPackagesFromProjects_EmptyProjectsList_ReturnsEmptyDictionary()
    {
        // Arrange
        var projects = new List<string>();

        // Act
        var result = _helpers.GetPackagesFromProjects(projects);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }
}
