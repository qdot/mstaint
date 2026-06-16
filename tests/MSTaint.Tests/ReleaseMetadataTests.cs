using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MSTaint.Tests;

public sealed class ReleaseMetadataTests
{
    [Fact]
    public void InstallerVersionDefaultsMatchBuildProperties()
    {
        var root = FindRepositoryRoot();
        var props = XDocument.Load(Path.Combine(root, "Directory.Build.props"));
        var propertyGroup = props.Root?.Element("PropertyGroup");

        var version = propertyGroup?.Element("Version")?.Value.Trim();
        var fileVersion = propertyGroup?.Element("FileVersion")?.Value.Trim();
        var installer = File.ReadAllText(Path.Combine(root, "installer", "MSTaint.iss"));

        Assert.False(string.IsNullOrWhiteSpace(version));
        Assert.False(string.IsNullOrWhiteSpace(fileVersion));
        Assert.Equal(version, ReadInnoDefine(installer, "AppVersion"));
        Assert.Equal(fileVersion, ReadInnoDefine(installer, "AppFileVersion"));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private static string ReadInnoDefine(string installerScript, string name)
    {
        var match = Regex.Match(
            installerScript,
            $"""(?m)^#define\s+{Regex.Escape(name)}\s+"(?<value>[^"]+)"\s*$""");

        Assert.True(match.Success, $"Could not find Inno Setup define '{name}'.");
        return match.Groups["value"].Value;
    }
}
