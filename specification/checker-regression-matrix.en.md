# Checker Regression Matrix

[日本語](checker-regression-matrix.md) | **English**

The four UPD Commander Checker implementations must regression-test the same rules with the same meaning and severity.

## Required common cases

| Case | Rule | Expected |
|---|---|---|
| dependency-ui-data | UPD101 | error |
| dependency-cross-application | UPD102 | error |
| data-commander-direct | UPD103 | warning |
| commander-loop | UPD201 | warning |
| commander-calculation | UPD202 | warning |
| commander-direct-work | UPD203 | error |
| multiple-inputs | UPD301 | attention |
| multiple-outputs | UPD302 | attention |
| substantial-compresser-opportunity | UPD303 | warning |
| responsibility-too-large | UPD401 | warning |
| multiple-behavior-types | UPD402 | warning |
| colocated-data-type | UPD403 | attention |
| externally-used-colocated-data-type | UPD404 | warning |

## Common Ignore cases

Each language implementation must independently verify:

- CLI/path ignore: the target file is excluded from scanning
- `.updcommanderignore` rule + path: only the specified rule is suppressed
- inline `upd: ignore CODE`: only the specified rule on the specified line is suppressed

## AST / syntax-analysis regression cases

Languages using AST or equivalent syntax analysis should commonly verify that:

- pseudo-code inside comments is not treated as a violation
- pseudo-code inside strings is not treated as a violation
- multiline declarations are parsed correctly
- nested types/blocks are not incorrectly counted as part of the outer responsibility unit

## Language-specific test entry points

- Python: `python -m unittest discover -s tests`
- Go: `go test ./...`
- C++: `ctest --test-dir build --output-on-failure`
- C#: `dotnet test ../upd_commander_checker_tests/UpdCommanderChecker.Tests.csproj`

When a new rule is added, add a case to this matrix and fix the same expected meaning/severity across all four language implementations. If language syntax makes identical input impossible, rule semantics and severity must still match.
