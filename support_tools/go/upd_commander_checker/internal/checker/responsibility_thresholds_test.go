package checker

import (
	"fmt"
	"go/parser"
	"go/token"
	"strings"
	"testing"
)

func responsibilityFindings(t *testing.T, source string) []Finding {
	t.Helper()
	fset := token.NewFileSet()
	file, err := parser.ParseFile(fset, "sample.go", source, 0)
	if err != nil {
		t.Fatal(err)
	}
	return checkResponsibilities(file, fset, "sample.go", strings.Split(source, "\n"), nil)
}

func hasResponsibilityWarning(findings []Finding) bool {
	for _, finding := range findings {
		if finding.Code == "UPD401" {
			return true
		}
	}
	return false
}

func TestResponsibility250LinesAllowed(t *testing.T) {
	fields := make([]string, 248)
	for index := range fields {
		fields[index] = fmt.Sprintf("Field%d int", index)
	}
	source := "package sample\ntype Example struct {\n" + strings.Join(fields, "\n") + "\n}"
	if hasResponsibilityWarning(responsibilityFindings(t, source)) {
		t.Fatal("250-line responsibility unit must be allowed")
	}
}

func TestResponsibility251LinesWarns(t *testing.T) {
	fields := make([]string, 249)
	for index := range fields {
		fields[index] = fmt.Sprintf("Field%d int", index)
	}
	source := "package sample\ntype Example struct {\n" + strings.Join(fields, "\n") + "\n}"
	if !hasResponsibilityWarning(responsibilityFindings(t, source)) {
		t.Fatal("251-line responsibility unit must warn")
	}
}

func TestResponsibility12MethodsAllowed(t *testing.T) {
	methods := make([]string, 12)
	for index := range methods {
		methods[index] = fmt.Sprintf("func (Example) Method%d() {}", index)
	}
	source := "package sample\ntype Example struct{}\n" + strings.Join(methods, "\n")
	if hasResponsibilityWarning(responsibilityFindings(t, source)) {
		t.Fatal("12 methods must be allowed")
	}
}

func TestResponsibility13MethodsWarns(t *testing.T) {
	methods := make([]string, 13)
	for index := range methods {
		methods[index] = fmt.Sprintf("func (Example) Method%d() {}", index)
	}
	source := "package sample\ntype Example struct{}\n" + strings.Join(methods, "\n")
	if !hasResponsibilityWarning(responsibilityFindings(t, source)) {
		t.Fatal("13 methods must warn")
	}
}
