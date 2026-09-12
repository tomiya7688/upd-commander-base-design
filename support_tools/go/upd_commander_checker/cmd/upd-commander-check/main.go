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
	var ignores stringList
	var warningsAsErrors bool
	flag.Var(&ignores, "ignore", "ignore path glob; repeatable")
	flag.BoolVar(&warningsAsErrors, "warnings-as-errors", false, "warnings fail the check")
	flag.Parse()

	target := "."
	if flag.NArg() > 0 {
		target = flag.Arg(0)
	}
	if _, err := os.Stat(target); err != nil {
		fmt.Printf("E UPD000 %s missing\n", target)
		os.Exit(2)
	}

	findings := checker.ScanPath(target, ignores)
	errors := 0
	warnings := 0
	for _, finding := range findings {
		level := "E"
		if finding.Severity == "warning" {
			level = "W"
			warnings++
		} else {
			errors++
		}
		fmt.Printf("%s %s %s:%d %s\n", level, finding.Code, finding.Path, finding.Line, finding.Message)
	}

	if errors > 0 || warningsAsErrors && warnings > 0 {
		fmt.Printf("FAIL e=%d w=%d\n", errors, warnings)
		os.Exit(1)
	}
	if warnings > 0 {
		fmt.Printf("OK w=%d\n", warnings)
		return
	}
	fmt.Println("OK")
}
