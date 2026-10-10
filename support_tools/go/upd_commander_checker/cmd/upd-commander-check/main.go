package main

import (
	"flag"
	"fmt"
	"os"
	"path/filepath"

	"upd_commander_checker/internal/checker"
)

// {
// 責務: [stringList: repeat指定可能なCLI文字列値を保持する]
// フィールド: [要素: CLIから追加された文字列]
// }
type stringList []string

// {
// 責務: [String: stringListをflag package向けに表示する]
// 処理: [1: 要素を文字列表現へ変換する]
// 引数: [items: 表示対象]
// 戻り値: [listの文字列表現]
// }
func (items *stringList) String() string { return fmt.Sprint([]string(*items)) }

// {
// 責務: [Set: CLIから受けた値をstringListへ追加する]
// 処理: [1: 値を末尾へ追加する]
// 引数: [value: 追加値]
// 戻り値: [成功時nil]
// }
func (items *stringList) Set(value string) error {
	*items = append(*items, value)
	return nil
}

// {
// 責務: [optionalPathFlag: 値なしでも有効化できるbaseline出力flagを表す]
// フィールド: [enabled: flag指定の有無, path: 任意の保存先]
// }
type optionalPathFlag struct {
	enabled bool
	path    string
}

// {
// 責務: [String: flagの現在値を返す]
// 処理: [1: 保存先pathを返す]
// 引数: [value: flag状態]
// 戻り値: [保存先path]
// }
func (value *optionalPathFlag) String() string { return value.path }

// {
// 責務: [IsBoolFlag: 値を省略したflag指定を許可する]
// 処理: [1: bool flagとして扱うことを示す]
// 引数: [value: flag状態]
// 戻り値: [常にtrue]
// }
func (value *optionalPathFlag) IsBoolFlag() bool { return true }

// {
// 責務: [Set: baseline出力flagを有効化し任意pathを保存する]
// 処理: [1: flagを有効にする, 2: true以外の値をpathとして保持する]
// 引数: [input: flag値]
// 戻り値: [成功時nil]
// }
func (value *optionalPathFlag) Set(input string) error {
	value.enabled = true
	if input != "true" {
		value.path = input
	}
	return nil
}

// {
// 責務: [optionalString: 値とCLIで明示指定されたかを保持する]
// フィールド: [value: 指定値, set: flag指定の有無]
// }
type optionalString struct {
	value string
	set   bool
}

// {
// 責務: [String: flagの現在値を返す]
// 処理: [1: value fieldを文字列として返す]
// 引数: [value: flag状態]
// 戻り値: [指定文字列]
// }
func (value *optionalString) String() string { return value.value }

// {
// 責務: [Set: flag値を保存し明示指定済みにする]
// 処理: [1: 値を保存する, 2: set状態をtrueにする]
// 引数: [input: flag値]
// 戻り値: [成功時nil]
// }
func (value *optionalString) Set(input string) error {
	value.value = input
	value.set = true
	return nil
}

// {
// 責務: [main: configを読込みCheckerを実行するprocess entry]
// 処理: [1: configを検証する, 2: CLI実行結果をprocess終了codeに反映する]
// 引数: [なし]
// 戻り値: [なし]
// }
func main() {
	config, configErr := checker.LoadConfig()
	if configErr != nil {
		fmt.Printf("CONFIG ERROR: %s\n", configErr)
		os.Exit(2)
	}
	os.Exit(runCLI(os.Args[1:], config))
}

// {
// 責務: [runCLI: scan・baseline比較・gate判定を実行して報告する]
// 処理: [1: flagとconfigを統合する, 2: Findingをscan・分類する, 3: gate結果を出力する]
// 引数: [args: CLI引数, config: 読込済み設定]
// 戻り値: [成功0、Finding gate失敗1、設定・実行エラー2]
// }
func runCLI(args []string, config checker.Config) int {
	var ignores stringList
	var output string
	var warningsAsErrors bool
	var attentionsAsErrors bool
	var writeBaseline optionalPathFlag
	var baselinePath string
	var failOn optionalString
	failOnScopeDefault := config.FailOnScope
	if failOnScopeDefault == "" {
		failOnScopeDefault = "all"
	}
	var failOnScope string
	var severityOverrides stringList
	flags := flag.NewFlagSet("upd-commander-check", flag.ContinueOnError)
	flags.SetOutput(os.Stderr)
	flags.Var(&ignores, "ignore", "ignore path glob; repeatable")
	flags.StringVar(&output, "output", "", "report output path")
	flags.BoolVar(&warningsAsErrors, "warnings-as-errors", false, "warnings fail the check")
	flags.BoolVar(&attentionsAsErrors, "attentions-as-errors", false, "attentions fail the check")
	flags.Var(&writeBaseline, "write-baseline", "write a baseline (optional path via --write-baseline=PATH)")
	flags.StringVar(&baselinePath, "baseline", "", "compare findings with a baseline JSON file")
	flags.Var(&failOn, "fail-on", "comma-separated severities that fail the check")
	flags.StringVar(&failOnScope, "fail-on-scope", failOnScopeDefault, "findings subject to the gate: all or new")
	flags.Var(&severityOverrides, "severity-override", "override a rule severity as UPDnnn=severity; repeatable")
	if err := flags.Parse(args); err != nil {
		return finishReport([]string{fmt.Sprintf("CLI ERROR: %s", err)}, "", 2)
	}
	if writeBaseline.enabled && baselinePath != "" {
		return finishReport([]string{"CLI ERROR: --write-baseline and --baseline are mutually exclusive"}, "", 2)
	}
	if failOnScope != "all" && failOnScope != "new" {
		return finishReport([]string{"CONFIG ERROR: invalid --fail-on-scope; expected all or new"}, "", 2)
	}
	if failOnScope == "new" && baselinePath == "" {
		return finishReport([]string{"CONFIG ERROR: fail_on_scope=new requires --baseline"}, "", 2)
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
	findings = checker.ApplySeverityOverrides(findings, overrides)
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
		line := fmt.Sprintf("%s %s %s:%d %s", level, finding.Code, finding.Path, finding.Line, finding.Message)
		fingerprint, fingerprintErr := checker.FindingFingerprint(finding.Code, finding.Path, finding.Symbol, finding.Context)
		if fingerprintErr != nil && baselinePath != "" {
			return finishReport([]string{fmt.Sprintf("BASELINE ERROR: %s", fingerprintErr)}, output, 2)
		}
		reason := checker.GateExceptionReason(finding, config.GateExceptions)
		suffix := ""
		if reason != "" {
			suffix = fmt.Sprintf(" [gate exception: %s]", reason)
		} else if failOnScope == "all" || statuses[fingerprint] == "NEW" {
			gateFindings = append(gateFindings, finding)
		}
		if baselinePath != "" {
			line = statuses[fingerprint] + " " + line
		}
		lines = append(lines, line+suffix)
	}
	for _, finding := range resolved {
		level := map[string]string{"error": "E", "warning": "W", "attention": "A"}[finding.Severity]
		if level == "" {
			level = "A"
		}
		lines = append(lines, fmt.Sprintf("RESOLVED %s %s %s:%d %s", level, finding.Rule, finding.Path, finding.Line, finding.Message))
	}

	if checker.ShouldFail(gateFindings, gate) {
		lines = append(lines, fmt.Sprintf("FAIL e=%d w=%d a=%d", errors, warnings, attentions))
		return finishReport(lines, output, 1)
	}
	if failOnScope == "new" {
		lines = append(lines, fmt.Sprintf("OK e=%d w=%d a=%d", errors, warnings, attentions))
	} else if warnings > 0 || attentions > 0 {
		lines = append(lines, fmt.Sprintf("OK w=%d a=%d", warnings, attentions))
	} else {
		lines = append(lines, "OK")
	}
	return finishReport(lines, output, 0)
}

// {
// 責務: [scanRoot: targetがfileかdirectoryかに応じscan rootを決める]
// 処理: [1: target metadataを調べる, 2: fileなら親directoryを返す]
// 引数: [target: scan対象]
// 戻り値: [scan root path]
// }
func scanRoot(target string) string {
	info, err := os.Stat(target)
	if err == nil && info.IsDir() {
		return target
	}
	return filepath.Dir(target)
}
