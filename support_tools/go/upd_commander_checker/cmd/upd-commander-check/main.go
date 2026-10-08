package main

import (
	"flag"
	"fmt"
	"os"

	"upd_commander_checker/internal/checker"
)

type stringList []string

func (items *stringList) String() string { return fmt.Sprint([]string(*items)) }
func (items *stringList) Set(value string) error {
	*items = append(*items, value)
	return nil
}

type optionalString struct {
	value string
	set   bool
}

func (value *optionalString) String() string { return value.value }
func (value *optionalString) Set(input string) error {
	value.value = input
	value.set = true
	return nil
}

func main() {
	config, configErr := checker.LoadConfig()
	if configErr != nil {
		fmt.Printf("CONFIG ERROR: %s\n", configErr)
		os.Exit(2)
	}
	var ignores stringList
	var output string
	var warningsAsErrors bool
	var attentionsAsErrors bool
	var failOn optionalString
	var severityOverrides stringList
	flag.Var(&ignores, "ignore", "ignore path glob; repeatable")
	flag.StringVar(&output, "output", "", "report output path")
	flag.BoolVar(&warningsAsErrors, "warnings-as-errors", false, "warnings fail the check")
	flag.BoolVar(&attentionsAsErrors, "attentions-as-errors", false, "attentions fail the check")
	flag.Var(&failOn, "fail-on", "comma-separated severities that fail the check")
	flag.Var(&severityOverrides, "severity-override", "override a rule severity as UPDnnn=severity; repeatable")
	flag.Parse()

	target := config.Input
	if flag.NArg() > 0 {
		target = flag.Arg(0)
	}
	if output == "" {
		output = config.Output
	}
	ignores = append(stringList(config.Ignore), ignores...)
	warningsAsErrors = warningsAsErrors || config.WarningsAsErrors
	explicitGate := failOn.set || config.FailOn != nil
	gate := []string{"error"}
	if config.FailOn != nil {
		gate = append([]string{}, (*config.FailOn)...)
	}
	if failOn.set {
		var valid bool
		gate, valid = checker.ParseFailOnArgument(failOn.value)
		if !valid {
			fmt.Fprintln(os.Stderr, "CONFIG ERROR: invalid --fail-on")
			os.Exit(2)
		}
	}
	if !explicitGate {
		if warningsAsErrors {
			gate = append(gate, "warning")
		}
		if attentionsAsErrors {
			gate = append(gate, "attention")
		}
	}
	overrides := make(map[string]string, len(config.SeverityOverrides)+len(severityOverrides))
	for rule, severity := range config.SeverityOverrides {
		overrides[rule] = severity
	}
	for _, item := range severityOverrides {
		rule, severity, valid := checker.ParseSeverityOverrideArgument(item)
		if !valid {
			fmt.Fprintln(os.Stderr, "CONFIG ERROR: invalid --severity-override; expected UPDnnn=severity")
			os.Exit(2)
		}
		overrides[rule] = severity
	}

	if _, err := os.Stat(target); err != nil {
		finish(finishInput{lines: []string{fmt.Sprintf("E UPD000 %s missing", target)}, output: output, code: 2})
	}

	options := checker.DefaultScanOptions()
	options.Upd301MaxInputs = config.Upd301MaxInputs
	options.FlatLayerMinFiles = config.FlatLayerMinFiles
	options.FlatLayerMinDirectPercent = config.FlatLayerMinDirectPercent
	options.ModelGroupMinItems = config.ModelGroupMinItems
	options.ModelGroupMinOccurrences = config.ModelGroupMinOccurrences
	options.CommonRoots = config.CommonRoots
	findings := checker.FilterEnabledFindings(checker.ScanPathWithOptions(target, ignores, options), config.EnabledRules)
	findings = checker.ApplySeverityOverrides(findings, overrides)
	errors := 0
	warnings := 0
	attentions := 0
	lines := []string{}
	gateFindings := make([]checker.Finding, 0, len(findings))
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
		reason := checker.GateExceptionReason(finding, config.GateExceptions)
		suffix := ""
		if reason != "" {
			suffix = fmt.Sprintf(" [gate exception: %s]", reason)
		} else {
			gateFindings = append(gateFindings, finding)
		}
		lines = append(lines, fmt.Sprintf("%s %s %s:%d %s%s", level, finding.Code, finding.Path, finding.Line, finding.Message, suffix))
	}

	if checker.ShouldFail(gateFindings, gate) {
		lines = append(lines, fmt.Sprintf("FAIL e=%d w=%d a=%d", errors, warnings, attentions))
		finish(finishInput{lines: lines, output: output, code: 1})
	}
	if warnings > 0 || attentions > 0 {
		lines = append(lines, fmt.Sprintf("OK w=%d a=%d", warnings, attentions))
	} else {
		lines = append(lines, "OK")
	}
	finish(finishInput{lines: lines, output: output, code: 0})
}

func finish(input finishInput) {
	os.Exit(finishReport(input.lines, input.output, input.code))
}
