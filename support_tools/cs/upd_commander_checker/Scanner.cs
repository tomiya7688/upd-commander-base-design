namespace UpdCommanderChecker;

internal static class Scanner
{
    internal static List<Finding> ScanPath(ScanPathInput input)
    {
        var targetIsDirectory = Directory.Exists(input.Target);
        var root = targetIsDirectory
            ? Path.GetFullPath(input.Target)
            : Path.GetDirectoryName(Path.GetFullPath(input.Target))
                ?? Directory.GetCurrentDirectory();
        var contextRoot = targetIsDirectory ? root : SingleFileContext.FindRoot(input.Target);
        IReadOnlyList<IgnoreRule> ignoreRules;
        try
        {
            ignoreRules = IgnoreRules.Load(root);
        }
        catch (Exception exception)
            when (exception
                    is IOException
                        or UnauthorizedAccessException
                        or System.Security.SecurityException
            )
        {
            return
            [
                new Finding(
                    ".updcommanderignore",
                    1,
                    "UPD001",
                    $"read failed: {exception.Message}"
                ),
            ];
        }

        var findings = new List<Finding>();
        var files = SourceFileWalker.Enumerate(
            new SourceFileWalkInput(input.Target, root, findings)
        );
        var includedFiles = new List<string>();
        var sources = new List<ParsedSource>();
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (
                input.CliIgnore.Any(pattern =>
                    IgnoreRules.GlobMatch(new GlobMatchInput(relative, pattern))
                ) || IgnoreRules.IsPathIgnored(new PathIgnoreCheckInput(relative, ignoreRules))
            )
            {
                continue;
            }

            includedFiles.Add(file);
            var parsed = SourceParser.Parse(new ScanFileInput(file, relative, ignoreRules));
            if (parsed.Finding is not null)
            {
                findings.Add(parsed.Finding);
                continue;
            }
            if (parsed.Source is not null)
            {
                sources.Add(parsed.Source);
            }
        }

        var semanticSources = sources.ToList();
        if (!targetIsDirectory && sources.Count > 0)
        {
            var contextFindings = new List<Finding>();
            var contextFiles = SourceFileWalker.Enumerate(
                new SourceFileWalkInput(contextRoot, contextRoot, contextFindings)
            );
            var targetFullPath = Path.GetFullPath(input.Target);
            foreach (var contextFile in contextFiles)
            {
                if (
                    string.Equals(
                        Path.GetFullPath(contextFile),
                        targetFullPath,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    continue;
                }
                var contextRelative = Path.GetRelativePath(contextRoot, contextFile)
                    .Replace('\\', '/');
                var parsed = SourceParser.Parse(
                    new ScanFileInput(contextFile, contextRelative, ignoreRules)
                );
                if (parsed.Source is not null)
                {
                    semanticSources.Add(parsed.Source);
                }
            }
        }

        var semanticProject = SemanticProject.Create(semanticSources);
        var modelOccurrences = new List<ModelGroupOccurrence>();
        foreach (var source in sources)
        {
            var classificationRelative = Path.GetRelativePath(contextRoot, source.File)
                .Replace('\\', '/');
            var module = Classifier.ClassifyPath(classificationRelative);
            findings.AddRange(
                CSharpAstAnalyzer.Analyze(
                    new AstAnalysisInput(
                        source.Root,
                        module,
                        source.Relative,
                        source.Lines,
                        ignoreRules,
                        semanticProject,
                        input.Upd301MaxInputs
                    )
                )
            );
            modelOccurrences.AddRange(
                ModelAttentionAnalyzer.Collect(
                    source,
                    module,
                    ignoreRules,
                    input.ModelGroupMinItems
                )
            );
        }
        findings.AddRange(
            ModelAttentionAnalyzer.BuildFindings(
                modelOccurrences,
                input.ModelGroupMinOccurrences
            )
        );
        findings.AddRange(
            DataTypeLocationRules.Check(
                new DataTypeLocationRuleContext(includedFiles, root, ignoreRules)
            )
        );
        if (targetIsDirectory)
        {
            findings.AddRange(
                CheckFlatLayers(
                    (
                        includedFiles,
                        root,
                        ignoreRules,
                        input.FlatLayerMinFiles,
                        input.FlatLayerMinDirectPercent
                    )
                )
            );
        }

        return findings
            .OrderBy(item => item.Path, StringComparer.Ordinal)
            .ThenBy(item => item.Line)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ToList();
    }

    private static readonly HashSet<string> FlatLayerExcludedDirectories =
    [
        "generated",
        "third_party",
        "vendor",
        "external",
        "build",
    ];

    private static readonly HashSet<string> ApplicationMarkers =
    [
        "app",
        "apps",
        "application",
        "applications",
        "feature",
        "features",
    ];

    private static readonly HashSet<string> LayerNames = ["ui", "process", "data"];

    private static IEnumerable<Finding> CheckFlatLayers(
        (
            IReadOnlyList<string> Files,
            string Root,
            IReadOnlyList<IgnoreRule> IgnoreRules,
            int MinFiles,
            int MinDirectPercent
        ) input
    )
    {
        var counts = new Dictionary<string, (int Total, int Direct)>(StringComparer.Ordinal);
        foreach (var file in input.Files)
        {
            var relative = Path.GetRelativePath(input.Root, file).Replace('\\', '/');
            var parts = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (
                parts
                    .Take(Math.Max(0, parts.Length - 1))
                    .Select(part => part.ToLowerInvariant())
                    .Any(FlatLayerExcludedDirectories.Contains)
            )
            {
                continue;
            }

            var layerIndex = FlatLayerRootIndex(parts);
            if (layerIndex < 0)
            {
                continue;
            }
            var layerRoot = string.Join('/', parts.Take(layerIndex + 1));
            counts.TryGetValue(layerRoot, out var count);
            count.Total++;
            if (parts.Length == layerIndex + 2)
            {
                count.Direct++;
            }
            counts[layerRoot] = count;
        }

        foreach (var (layerRoot, count) in counts)
        {
            if (
                count.Total < input.MinFiles
                || count.Direct * 100 < count.Total * input.MinDirectPercent
                || IgnoreRules.IsIgnored(
                    new IgnoreCheckInput(layerRoot, "UPD405", string.Empty, input.IgnoreRules)
                )
            )
            {
                continue;
            }
            yield return new Finding(
                layerRoot,
                1,
                "UPD405",
                "large flat layer reduces navigability; consider grouping related responsibilities",
                "attention"
            );
        }
    }

    private static int FlatLayerRootIndex(IReadOnlyList<string> parts)
    {
        var scopeStart = 0;
        for (var index = 0; index + 2 < parts.Count; index++)
        {
            if (ApplicationMarkers.Contains(parts[index].ToLowerInvariant()))
            {
                scopeStart = index + 2;
            }
        }

        var layerIndex = -1;
        for (var index = scopeStart; index + 1 < parts.Count; index++)
        {
            if (LayerNames.Contains(parts[index].ToLowerInvariant()))
            {
                layerIndex = index;
            }
        }
        return layerIndex;
    }
}
