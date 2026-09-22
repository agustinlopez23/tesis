using System.Security.Cryptography;

namespace Marketplace.Evaluation;

internal static class FreezeFiles
{
    internal static readonly string[] Projects = ["App/backend/src/Marketplace.BLL", "App/backend/src/Marketplace.DTOs",
        "App/backend/src/Marketplace.Models", "App/backend/tools/Marketplace.Evaluation", "App/backend/tests/Marketplace.BLL.Tests"];
    private const string Documents = "Doc/evaluation/business-ranking-final/";
    internal static SortedDictionary<string, byte[]> Sources(string root)
    {
        var files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        void Add(string relative)
        {
            var path = SafePath(root, relative);
            if (!File.Exists(path)) throw new ArgumentException($"Required source file missing: {relative}.");
            files[relative] = File.ReadAllBytes(path);
        }
        foreach (var project in Projects)
        {
            var folder = SafePath(root, project);
            if (!Directory.Exists(folder)) throw new ArgumentException($"Required project missing: {project}.");
            Add(project + "/" + Path.GetFileName(project) + ".csproj");
            var paths = SourcePaths(folder).ToArray();
            if (!paths.Any(p => Path.GetExtension(p) == ".cs")) throw new ArgumentException($"Project has no sources: {project}.");
            foreach (var path in paths) Add(Path.GetRelativePath(root, path).Replace('\\', '/'));
        }
        foreach (var name in new[] { "protocol.md", "plan.json", "human-review.md", "templates/queries.csv",
            "templates/candidates.csv", "templates/judgments.csv", "templates/resolutions.csv" }) Add(Documents + name);
        Add("App/backend/global.json");
        var parents = Projects.SelectMany(p => p.Split('/').Select((_, i) => string.Join("/", p.Split('/').Take(i + 1)) + "/"))
            .Append("").Distinct(StringComparer.Ordinal);
        foreach (var parent in parents)
            foreach (var name in new[] { "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props" })
                if (File.Exists(Path.Combine(root, parent + name))) Add(parent + name);
        return files;
    }
    private static IEnumerable<string> SourcePaths(string folder)
    {
        CheckNoLinks(folder);
        foreach (var entry in Directory.EnumerateFileSystemEntries(folder).Order(StringComparer.Ordinal))
        {
            if (Path.GetFileName(entry) is "bin" or "obj" or ".git") continue;
            if (Directory.Exists(entry)) { foreach (var path in SourcePaths(entry)) yield return path; }
            else if (Path.GetExtension(entry) is ".cs" or ".csproj") { CheckNoLinks(entry); yield return entry; }
        }
    }
    internal static IEnumerable<string> Tree(string folder)
    {
        CheckNoLinks(folder);
        foreach (var entry in Directory.EnumerateFileSystemEntries(folder).Order(StringComparer.Ordinal))
        {
            CheckNoLinks(entry);
            if (Directory.Exists(entry)) { foreach (var path in Tree(entry)) yield return path; }
            else yield return entry;
        }
    }
    internal static string SafePath(string root, string relative)
    {
        if (Path.IsPathRooted(relative) || relative.Contains('\\') || relative.Split('/').Any(s =>
            s.Length == 0 || s is "." or ".." || s.TrimEnd(' ', '.') != s || s.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            throw new ArgumentException("Artifact path must be a canonical relative path.");
        var path = Path.GetFullPath(Path.Combine(root, relative));
        if (!Within(root, path)) throw new ArgumentException("Artifact path escapes its directory.");
        CheckNoLinks(path);
        return path;
    }
    internal static void CheckNoLinks(string path)
    {
        for (var current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
            if (Path.Exists(current) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("Snapshot paths cannot contain symbolic links or junctions.");
    }
    internal static bool Within(string parent, string child) => Path.GetFullPath(child).TrimEnd(Path.DirectorySeparatorChar)
        .StartsWith(Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
        || string.Equals(Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(child).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
    internal static void Output(string output, params string[] inputs)
    {
        CheckNoLinks(output);
        if (inputs.Any(input => Within(input, output) || Within(output, input))) throw new ArgumentException("Output and input paths must not overlap.");
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any() || File.Exists(output))
            throw new IOException("Output must be empty; existing evidence will not be overwritten.");
    }
    internal static void Write(string root, string relative, byte[] bytes)
    {
        var path = SafePath(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var file = new FileStream(path, FileMode.CreateNew);
        file.Write(bytes);
    }
    internal static string HashFile(string path)
    {
        using var file = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(file)).ToLowerInvariant();
    }
    internal static SortedDictionary<string, string> Hashes(string root) => new(Tree(root).ToDictionary(
        path => Path.GetRelativePath(root, path).Replace('\\', '/'), HashFile), StringComparer.Ordinal);
}
