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
        foreach (var source in sources)
        {
            var classificationRelative = Path.GetRelativePath(contextRoot, source.File)
                .Replace('\\', '/');
            findings.AddRange(
                CSharpAstAnalyzer.Analyze(
                    new AstAnalysisInput(
                        source.Root,
                        Classifier.ClassifyPath(classificationRelative),
                        source.Relative,
                        source.Lines,
                        ignoreRules,
                        semanticProject
                    )
                )
            );
        }
        findings.AddRange(
            DataTypeLocationRules.Check(
                new DataTypeLocationRuleContext(includedFiles, root, ignoreRules)
            )
        );

        return findings
            .OrderBy(item => item.Path, StringComparer.Ordinal)
            .ThenBy(item => item.Line)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ToList();
    }
}
