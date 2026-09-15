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
	flag.Var(&ignores, "ignore", "ignore path glob; repeatable")
	flag.StringVar(&output, "output", "", "report output path")
	flag.BoolVar(&warningsAsErrors, "warnings-as-errors", false, "warnings fail the check")
	flag.BoolVar(&attentionsAsErrors, "attentions-as-errors", false, "attentions fail the check")
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

	if _, err := os.Stat(target); err != nil {
		finish([]string{fmt.Sprintf("E UPD000 %s missing", target)}, output, 2)
	}

	findings := checker.FilterEnabledFindings(checker.ScanPath(target, ignores), config.EnabledRules)
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
		lines = append(lines, fmt.Sprintf("%s %s %s:%d %s", level, finding.Code, finding.Path, finding.Line, finding.Message))
	}

	if errors > 0 || warningsAsErrors && warnings > 0 || attentionsAsErrors && attentions > 0 {
		lines = append(lines, fmt.Sprintf("FAIL e=%d w=%d a=%d", errors, warnings, attentions))
		finish(lines, output, 1)
	}
	if warnings > 0 || attentions > 0 {
		lines = append(lines, fmt.Sprintf("OK w=%d a=%d", warnings, attentions))
	} else {
		lines = append(lines, "OK")
	}
	finish(lines, output, 0)
}

func finish(lines []string, output string, code int) {
	os.Exit(finishReport(lines, output, code))
}
