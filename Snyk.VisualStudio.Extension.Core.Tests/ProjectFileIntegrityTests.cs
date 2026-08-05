// ABOUTME: Guards the hand-maintained project file lists that the Windows build depends on.
// Snyk.VisualStudio.Extension.2022.csproj is a legacy (non-SDK) project: every source file has to
// be listed explicitly, and a file that is added to disk but not to the csproj compiles fine
// locally in some editors yet silently disappears from the VSIX. The cross-platform test project
// has the same exposure through its two .props file lists. These checks run on Linux, so the
// mistake is caught long before a Windows-only build.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Snyk.VisualStudio.Extension.Tests
{
    public class ProjectFileIntegrityTests
    {
        private const string ExtensionProject = "Snyk.VisualStudio.Extension.2022";
        private const string WindowsTestProject = "Snyk.VisualStudio.Extension.Tests";
        private const string CoreTestProject = "Snyk.VisualStudio.Extension.Core.Tests";

        private static readonly Regex IncludeAttribute = new Regex(
            @"<(?:Compile|Page|EmbeddedResource|Content|None|Resource|VSCTCompile|ApplicationDefinition)\s+[^>]*Include=""(?<path>[^""]+)""",
            RegexOptions.Compiled);

        [Fact]
        public void ExtensionCsproj_ListsEverySourceFileOnDisk()
        {
            var projectDir = Path.Combine(RepositoryRoot(), ExtensionProject);
            var listed = IncludedPaths(Path.Combine(projectDir, ExtensionProject + ".csproj"));

            var missing = SourceFilesUnder(projectDir)
                .Where(relativePath => !listed.Contains(relativePath))
                .OrderBy(relativePath => relativePath, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Assert.True(
                missing.Count == 0,
                $"{ExtensionProject}.csproj is a legacy project and must list every source file. " +
                $"Add <Compile Include=\"...\"/> (or <Page .../> for XAML) entries for: {string.Join(", ", missing)}");
        }

        [Fact]
        public void ExtensionCsproj_DoesNotReferenceDeletedFiles()
        {
            var projectDir = Path.Combine(RepositoryRoot(), ExtensionProject);

            var absent = IncludedPaths(Path.Combine(projectDir, ExtensionProject + ".csproj"))
                .Where(relativePath => relativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                                       || relativePath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
                .Where(relativePath => !File.Exists(Path.Combine(projectDir, relativePath.Replace('/', Path.DirectorySeparatorChar))))
                .OrderBy(relativePath => relativePath, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Assert.True(
                absent.Count == 0,
                $"{ExtensionProject}.csproj references files that no longer exist: {string.Join(", ", absent)}");
        }

        [Theory]
        [InlineData("Snyk.VisualStudio.Extension.Core.props", ExtensionProject)]
        [InlineData("Snyk.VisualStudio.Extension.Core.Tests.props", WindowsTestProject)]
        public void CoreSourceList_OnlyReferencesFilesThatExist(string propsFileName, string sourceProject)
        {
            var sourceDir = Path.Combine(RepositoryRoot(), sourceProject);

            var absent = IncludedPaths(Path.Combine(RepositoryRoot(), CoreTestProject, propsFileName))
                .Select(relativePath => Regex.Replace(relativePath, @"^\$\([A-Za-z]+\)", string.Empty))
                .Where(relativePath => !File.Exists(Path.Combine(sourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar))))
                .OrderBy(relativePath => relativePath, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Assert.True(
                absent.Count == 0,
                $"{propsFileName} lists files that no longer exist under {sourceProject}: {string.Join(", ", absent)}");
        }

        [Fact]
        public void CoreSourceList_DoesNotIncludeVisualStudioOnlyParts()
        {
            // *.Vs.cs is the convention for the Visual Studio bound half of a partial type. Linking
            // one into the cross-platform build would drag the VS SDK in with it.
            var offenders = IncludedPaths(Path.Combine(RepositoryRoot(), CoreTestProject, "Snyk.VisualStudio.Extension.Core.props"))
                .Where(relativePath => relativePath.EndsWith(".Vs.cs", StringComparison.Ordinal)
                                       || relativePath.EndsWith(".xaml.cs", StringComparison.Ordinal))
                .OrderBy(relativePath => relativePath, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Assert.True(
                offenders.Count == 0,
                $"Visual Studio only sources must stay out of the cross-platform build: {string.Join(", ", offenders)}");
        }

        private static ISet<string> IncludedPaths(string projectFilePath)
        {
            var contents = File.ReadAllText(projectFilePath);

            return new HashSet<string>(
                IncludeAttribute.Matches(contents)
                    .Cast<Match>()
                    .Select(match => match.Groups["path"].Value.Replace('\\', '/')),
                StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> SourceFilesUnder(string projectDir)
        {
            return Directory
                .EnumerateFiles(projectDir, "*.*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                               || path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
                .Where(path => !IsBuildOutput(path, projectDir))
                .Select(path => GetRelativePath(projectDir, path));
        }

        private static bool IsBuildOutput(string path, string projectDir)
        {
            var relative = GetRelativePath(projectDir, path);
            return relative.StartsWith("bin/", StringComparison.OrdinalIgnoreCase)
                   || relative.StartsWith("obj/", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetRelativePath(string basePath, string fullPath) =>
            fullPath.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace('\\', '/');

        /// <summary>
        /// Walks up from the test assembly to the directory holding the solution file, so the
        /// checks work from any working directory and on any platform.
        /// </summary>
        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "snyk-visual-studio-plugin.sln")))
            {
                directory = directory.Parent;
            }

            Assert.NotNull(directory);
            return directory.FullName;
        }
    }
}
