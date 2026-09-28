package main

import (
	"flag"
	"fmt"
	"os"
	"path/filepath"

	"upd_commander_checker/internal/checker"
)

type stringList []string

func (items *stringList) String() string { return fmt.Sprint([]string(*items)) }
func (items *stringList) Set(value string) error {
	*items = append(*items, value)
	return nil
}

type optionalPathFlag struct {
	enabled bool
	path    string
}

func (value *optionalPathFlag) String() string   { return value.path }
func (value *optionalPathFlag) IsBoolFlag() bool { return true }
func (value *optionalPathFlag) Set(input string) error {
	value.enabled = true
	if input != "true" {
		value.path = input
	}
	return nil
}

func main() {
	config, configErr := checker.LoadConfig()
	if configErr != nil {
		fmt.Printf("CONFIG ERROR: %s\n", configErr)
		os.Exit(2)
	}
	os.Exit(runCLI(os.Args[1:], config))
}

func runCLI(args []string, config checker.Config) int {
	var ignores stringList
	var output string
	var warningsAsErrors bool
	var attentionsAsErrors bool
	var writeBaseline optionalPathFlag
	var baselinePath string
	flags := flag.NewFlagSet("upd-commander-check", flag.ContinueOnError)
	flags.SetOutput(os.Stderr)
	flags.Var(&ignores, "ignore", "ignore path glob; repeatable")
	flags.StringVar(&output, "output", "", "report output path")
	flags.BoolVar(&warningsAsErrors, "warnings-as-errors", false, "warnings fail the check")
	flags.BoolVar(&attentionsAsErrors, "attentions-as-errors", false, "attentions fail the check")
	flags.Var(&writeBaseline, "write-baseline", "write a baseline (optional path via --write-baseline=PATH)")
	flags.StringVar(&baselinePath, "baseline", "", "compare findings with a baseline JSON file")
	if err := flags.Parse(args); err != nil {
		return finishReport([]string{fmt.Sprintf("CLI ERROR: %s", err)}, "", 2)
	}
	if writeBaseline.enabled && baselinePath != "" {
		return finishReport([]string{"CLI ERROR: --write-baseline and --baseline are mutually exclusive"}, "", 2)
	}

	target := config.Input
	if flags.NArg() > 0 {
		target = flags.Arg(0)
	}
	if output == "" {
		output = config.Output
	}
	ignores = append(stringList(config.Ignore), ignores...)
	warningsAsErrors = warningsAsErrors || config.WarningsAsErrors

	if _, err := os.Stat(target); err != nil {
		return finishReport([]string{fmt.Sprintf("E UPD000 %s missing", target)}, output, 2)
	}

	options := checker.DefaultScanOptions()
	options.Upd301MaxInputs = config.Upd301MaxInputs
	options.FlatLayerMinFiles = config.FlatLayerMinFiles
	options.FlatLayerMinDirectPercent = config.FlatLayerMinDirectPercent
	options.ModelGroupMinItems = config.ModelGroupMinItems
	options.ModelGroupMinOccurrences = config.ModelGroupMinOccurrences
	options.CommonRoots = config.CommonRoots
	findings := checker.FilterEnabledFindings(checker.ScanPathWithOptions(target, ignores, options), config.EnabledRules)
	baselineFindings := make([]checker.BaselineEntry, 0, len(findings))
	for _, finding := range findings {
		baselineFindings = append(baselineFindings, checker.BaselineEntry{
			Rule: finding.Code, Path: finding.Path, Symbol: finding.Symbol, Context: finding.Context,
			Severity: finding.Severity, Line: finding.Line, Message: finding.Message,
		})
	}
	statuses := map[string]string{}
	resolved := []checker.BaselineEntry{}
	if writeBaseline.enabled {
		if writeBaseline.path == "" {
			writeBaseline.path = filepath.Join(scanRoot(target), ".upd-baseline.json")
		}
		if err := checker.WriteBaseline(writeBaseline.path, baselineFindings); err != nil {
			return finishReport([]string{fmt.Sprintf("BASELINE ERROR: %s", err)}, output, 2)
		}
	}
	if baselinePath != "" {
		baseline, err := checker.LoadBaseline(baselinePath)
		if err != nil {
			return finishReport([]string{fmt.Sprintf("BASELINE ERROR: %s", err)}, output, 2)
		}
		comparison, err := checker.CompareBaseline(baselineFindings, baseline)
		if err != nil {
			return finishReport([]string{fmt.Sprintf("BASELINE ERROR: %s", err)}, output, 2)
		}
		for _, finding := range comparison.New {
			statuses[finding.Fingerprint] = "NEW"
		}
		for _, finding := range comparison.Existing {
			statuses[finding.Fingerprint] = "EXISTING"
		}
		resolved = comparison.Resolved
	}
	errors := 0
	warnings := 0
	attentions := 0
	lines := []string{}
	for _, finding := range findings {
		level := "A"
		switch finding.Severity {
		case "error":
			level = "E"
			errors++
		case "warning":
			level = "W"
			warnings++
		default:
			attentions++
		}
		line := fmt.Sprintf("%s %s %s:%d %s", level, finding.Code, finding.Path, finding.Line, finding.Message)
		if baselinePath != "" {
			fingerprint, err := checker.FindingFingerprint(finding.Code, finding.Path, finding.Symbol, finding.Context)
			if err != nil {
				return finishReport([]string{fmt.Sprintf("BASELINE ERROR: %s", err)}, output, 2)
			}
			line = statuses[fingerprint] + " " + line
		}
		lines = append(lines, line)
	}
	for _, finding := range resolved {
		level := map[string]string{"error": "E", "warning": "W", "attention": "A"}[finding.Severity]
		if level == "" {
			level = "A"
		}
		lines = append(lines, fmt.Sprintf("RESOLVED %s %s %s:%d %s", level, finding.Rule, finding.Path, finding.Line, finding.Message))
	}

	if errors > 0 || warningsAsErrors && warnings > 0 || attentionsAsErrors && attentions > 0 {
		lines = append(lines, fmt.Sprintf("FAIL e=%d w=%d a=%d", errors, warnings, attentions))
		return finishReport(lines, output, 1)
	}
	if warnings > 0 || attentions > 0 {
		lines = append(lines, fmt.Sprintf("OK w=%d a=%d", warnings, attentions))
	} else {
		lines = append(lines, "OK")
	}
	return finishReport(lines, output, 0)
}

func scanRoot(target string) string {
	info, err := os.Stat(target)
	if err == nil && info.IsDir() {
		return target
	}
	return filepath.Dir(target)
}
