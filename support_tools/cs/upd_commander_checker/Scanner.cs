namespace UpdCommanderChecker;

internal static class Scanner
{
    internal static List<Finding> ScanPath(ScanPathInput input)
    {
        var root = Directory.Exists(input.Target)
            ? Path.GetFullPath(input.Target)
            : Path.GetDirectoryName(Path.GetFullPath(input.Target))
                ?? Directory.GetCurrentDirectory();
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

        var semanticProject = SemanticProject.Create(sources);
        foreach (var source in sources)
        {
            findings.AddRange(
                CSharpAstAnalyzer.Analyze(
                    new AstAnalysisInput(
                        source.Root,
                        Classifier.ClassifyPath(source.Relative),
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
