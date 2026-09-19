package checker

import (
	"go/ast"
	"go/parser"
	"go/token"
	"strconv"
	"strings"
)

type ModelGroupOccurrence struct {
	Path          string
	Line          int
	ApplicationID string
	Layer         string
	Kind          string
	Items         []string
}

func (occurrence ModelGroupOccurrence) Signature() string {
	return occurrence.ApplicationID + "\x1f" +
		occurrence.Layer + "\x1f" +
		occurrence.Kind + "\x1f" +
		strings.Join(occurrence.Items, "\x1e")
}

func collectModelGroupOccurrences(
	path string,
	relative string,
	classificationPath string,
	rules []IgnoreRule,
	minItems int,
) []ModelGroupOccurrence {
	fset := token.NewFileSet()
	file, err := parser.ParseFile(fset, path, nil, parser.ParseComments)
	if err != nil {
		return nil
	}
	source := ClassifyPath(classificationPath)
	lines := readLines(path)
	occurrences := []ModelGroupOccurrence{}

	add := func(node ast.Node, kind string, items []string) {
		if len(items) < minItems || !allUnique(items) {
			return
		}
		line := fset.Position(node.Pos()).Line
		lineText := ""
		if line > 0 && line <= len(lines) {
			lineText = lines[line-1]
		}
		if IsIgnored(relative, "UPD406", lineText, rules) {
			return
		}
		occurrences = append(occurrences, ModelGroupOccurrence{
			Path:          relative,
			Line:          line,
			ApplicationID: source.ApplicationID,
			Layer:         source.Layer,
			Kind:          kind,
			Items:         items,
		})
	}

	for _, declaration := range file.Decls {
		function, ok := declaration.(*ast.FuncDecl)
		if !ok {
			continue
		}
		items := parameterItemKeys(function.Type.Params)
		add(function, "parameters", items)
	}

	ast.Inspect(file, func(node ast.Node) bool {
		switch current := node.(type) {
		case *ast.ReturnStmt:
			if items, ok := expressionItemKeys(current.Results); ok {
				add(current, "tuple", items)
			}
			addParallelGroups(current, fset, source, relative, lines, rules, minItems, &occurrences)
		case *ast.AssignStmt:
			if items, ok := expressionItemKeys(current.Rhs); ok {
				add(current, "tuple", items)
			}
			addParallelGroups(current, fset, source, relative, lines, rules, minItems, &occurrences)
		case *ast.ExprStmt:
			addParallelGroups(current, fset, source, relative, lines, rules, minItems, &occurrences)
		}
		return true
	})
	return occurrences
}

func parameterItemKeys(fields *ast.FieldList) []string {
	if fields == nil {
		return nil
	}
	items := []string{}
	for _, field := range fields.List {
		for _, name := range field.Names {
			items = append(items, normalizeModelItem(name.Name))
		}
	}
	return items
}

func expressionItemKeys(expressions []ast.Expr) ([]string, bool) {
	items := make([]string, 0, len(expressions))
	for _, expression := range expressions {
		key := modelExpressionKey(expression)
		if key == "" {
			return nil, false
		}
		items = append(items, key)
	}
	return items, true
}

func modelExpressionKey(expression ast.Expr) string {
	switch current := expression.(type) {
	case *ast.Ident:
		return normalizeModelItem(current.Name)
	case *ast.SelectorExpr:
		return normalizeModelItem(current.Sel.Name)
	case *ast.IndexExpr:
		return modelExpressionKey(current.X)
	case *ast.IndexListExpr:
		return modelExpressionKey(current.X)
	default:
		return ""
	}
}

func addParallelGroups(
	node ast.Node,
	fset *token.FileSet,
	source ModuleInfo,
	relative string,
	lines []string,
	rules []IgnoreRule,
	minItems int,
	occurrences *[]ModelGroupOccurrence,
) {
	groups := map[string][]string{}
	ast.Inspect(node, func(child ast.Node) bool {
		indexed, ok := child.(*ast.IndexExpr)
		if !ok {
			return true
		}
		collection := modelExpressionKey(indexed.X)
		index := indexExpressionKey(indexed.Index)
		if collection != "" && index != "" {
			groups[index] = append(groups[index], collection)
		}
		return true
	})
	for _, rawItems := range groups {
		items := uniquePreservingOrder(rawItems)
		if len(items) < minItems {
			continue
		}
		line := fset.Position(node.Pos()).Line
		lineText := ""
		if line > 0 && line <= len(lines) {
			lineText = lines[line-1]
		}
		if IsIgnored(relative, "UPD406", lineText, rules) {
			continue
		}
		*occurrences = append(*occurrences, ModelGroupOccurrence{
			Path:          relative,
			Line:          line,
			ApplicationID: source.ApplicationID,
			Layer:         source.Layer,
			Kind:          "parallel_collection",
			Items:         items,
		})
	}
}

func indexExpressionKey(expression ast.Expr) string {
	switch current := expression.(type) {
	case *ast.Ident:
		return "name:" + normalizeModelItem(current.Name)
	case *ast.BasicLit:
		return "literal:" + current.Kind.String() + ":" + current.Value
	case *ast.SelectorExpr:
		return "selector:" + normalizeModelItem(current.Sel.Name)
	default:
		return "node:" + strconv.Itoa(int(expression.Pos()))
	}
}

func normalizeModelItem(value string) string {
	return strings.ToLower(strings.TrimLeft(value, "_"))
}

func allUnique(items []string) bool {
	seen := map[string]bool{}
	for _, item := range items {
		if item == "" || seen[item] {
			return false
		}
		seen[item] = true
	}
	return true
}

func uniquePreservingOrder(items []string) []string {
	seen := map[string]bool{}
	result := []string{}
	for _, item := range items {
		if item == "" || seen[item] {
			continue
		}
		seen[item] = true
		result = append(result, item)
	}
	return result
}

func modelAttentionFindings(occurrences []ModelGroupOccurrence, minOccurrences int) []Finding {
	groups := map[string][]ModelGroupOccurrence{}
	for _, occurrence := range occurrences {
		groups[occurrence.Signature()] = append(groups[occurrence.Signature()], occurrence)
	}

	findings := []Finding{}
	for _, group := range groups {
		if len(group) < minOccurrences {
			continue
		}
		first := group[0]
		for _, occurrence := range group[1:] {
			if occurrence.Path < first.Path || occurrence.Path == first.Path && occurrence.Line < first.Line {
				first = occurrence
			}
		}
		findings = append(findings, Finding{
			Path:     first.Path,
			Line:     first.Line,
			Code:     "UPD406",
			Message:  "repeated value group may benefit from a Model/DTO; items=" + strings.Join(first.Items, ",") + " occurrences=" + strconv.Itoa(len(group)) + " kind=" + first.Kind,
			Severity: "attention",
		})
	}
	return findings
}
