package checker

import (
	"bufio"
	"go/ast"
	"go/parser"
	"go/token"
	"os"
	"path"
	"path/filepath"
	"strconv"
	"strings"
)

func checkDataTypeLocations(paths []string, root string, rules []IgnoreRule) []Finding {
	parsed := parseDataFiles(paths, root)
	methodCounts := map[string]int{}
	for _, item := range parsed {
		for _, declaration := range item.file.Decls {
			function, ok := declaration.(*ast.FuncDecl)
			if !ok || function.Recv == nil || len(function.Recv.List) == 0 {
				continue
			}
			name := receiverName(function.Recv.List[0].Type)
			if name != "" {
				methodCounts[dataTypeKey(item.path, item.packageName, name)]++
			}
		}
	}

	var candidates []dataOnlyType
	for _, item := range parsed {
		var types []*ast.TypeSpec
		for _, declaration := range item.file.Decls {
			gen, ok := declaration.(*ast.GenDecl)
			if !ok || gen.Tok != token.TYPE {
				continue
			}
			for _, spec := range gen.Specs {
				typeSpec, ok := spec.(*ast.TypeSpec)
				if ok {
					types = append(types, typeSpec)
				}
		}
		if len(types) < 2 {
			continue
		}
		for _, typeSpec := range types {
			if _, ok := typeSpec.Type.(*ast.StructType); !ok {
				continue
			}
			if methodCounts[dataTypeKey(item.path, item.packageName, typeSpec.Name.Name)] != 0 {
				continue
			}
			candidates = append(candidates, dataOnlyType{
				path: item.path,
				rel: item.rel,
				name: typeSpec.Name.Name,
				line: item.fset.Position(typeSpec.Pos()).Line,
			})
		}
	}

	var findings []Finding
	for _, item := range candidates {
		code := "UPD403"
		severity := "attention"
		message := "data-only type " + item.name + " shares a file with another type"
		if dataTypeReferencedElsewhere(item, parsed, root) {
			code = "UPD404"
			severity = "warning"
			message = "data-only type " + item.name + " shares a file with another type and is referenced from another file"
		}
		addFinding(&findings, item.rel, item.line, code, message, severity, readLines(item.path), rules)
	}
	return findings
}

func parseDataFiles(paths []string, root string) []parsedDataFile {
	var result []parsedDataFile
	for _, path := range paths {
		fset := token.NewFileSet()
		file, err := parser.ParseFile(fset, path, nil, 0)
		if err != nil {
			continue
		}
		rel, err := filepath.Rel(root, path)
		if err != nil {
			rel = path
		}
		result = append(result, parsedDataFile{path, filepath.ToSlash(rel), file, fset, file.Name.Name})
	}
	return result
}

func dataTypeReferencedElsewhere(item dataOnlyType, files []parsedDataFile, root string) bool {
	candidateDir := filepath.Clean(filepath.Dir(item.path))
	candidatePackage := ""
	for _, source := range files {
		if source.path == item.path {
			candidatePackage = source.packageName
			break
		}
	}
	modulePath := goModulePath(root)
	for _, other := range files {
		if other.path == item.path {
			continue
		}
		if filepath.Clean(filepath.Dir(other.path)) == candidateDir && other.packageName == candidatePackage {
			if fileReferencesLocalType(other.file, item.name) {
				return true
			}
			continue
		}
		aliases := importedCandidateAliases(other.file, candidateDir, root, modulePath)
		if len(aliases) == 0 {
			continue
		}
		found := false
		ast.Inspect(other.file, func(node ast.Node) bool {
			selector, ok := node.(*ast.SelectorExpr)
			if !ok || selector.Sel.Name != item.name {
				return !found
			}
			identifier, ok := selector.X.(*ast.Ident)
			if ok && aliases[identifier.Name] {
				found = true
				return false
			}
			return !found
		})
		if found {
			return true
		}
	}
	return false
}

func fileReferencesLocalType(file *ast.File, name string) bool {
	found := false
	ast.Inspect(file, func(node ast.Node) bool {
		if found {
			return false
		}
		switch value := node.(type) {
		case *ast.Field:
			found = typeExprReferencesName(value.Type, name)
		case *ast.ValueSpec:
			found = value.Type != nil && typeExprReferencesName(value.Type, name)
		case *ast.CompositeLit:
			found = typeExprReferencesName(value.Type, name)
		case *ast.TypeAssertExpr:
			found = value.Type != nil && typeExprReferencesName(value.Type, name)
		}
		return !found
	})
	return found
}

func typeExprReferencesName(expr ast.Expr, name string) bool {
	if expr == nil {
		return false
	}
	switch value := expr.(type) {
	case *ast.Ident:
		return value.Name == name
	case *ast.StarExpr:
		return typeExprReferencesName(value.X, name)
	case *ast.ArrayType:
		return typeExprReferencesName(value.Elt, name)
	case *ast.MapType:
		return typeExprReferencesName(value.Key, name) || typeExprReferencesName(value.Value, name)
	case *ast.ChanType:
		return typeExprReferencesName(value.Value, name)
	case *ast.Ellipsis:
		return typeExprReferencesName(value.Elt, name)
	case *ast.IndexExpr:
		return typeExprReferencesName(value.X, name) || typeExprReferencesName(value.Index, name)
	case *ast.IndexListExpr:
		if typeExprReferencesName(value.X, name) {
			return true
		}
		for _, index := range value.Indices {
			if typeExprReferencesName(index, name) {
				return true
			}
		}
	case *ast.ParenExpr:
		return typeExprReferencesName(value.X, name)
	}
	return false
}

func importedCandidateAliases(file *ast.File, candidateDir string, root string, modulePath string) map[string]bool {
	aliases := map[string]bool{}
	relDir, err := filepath.Rel(root, candidateDir)
	if err != nil {
		return aliases
	}
	relImport := strings.Trim(filepath.ToSlash(relDir), "/")
	expected := relImport
	if modulePath != "" && relImport != "." && relImport != "" {
		expected = strings.TrimSuffix(modulePath, "/") + "/" + relImport
	} else if modulePath != "" && (relImport == "." || relImport == "") {
		expected = modulePath
	}
	for _, imported := range file.Imports {
		importPath, err := strconv.Unquote(imported.Path.Value)
		if err != nil || !matchesCandidateImport(importPath, expected, relImport) {
			continue
		}
		alias := path.Base(importPath)
		if imported.Name != nil {
			if imported.Name.Name == "_" || imported.Name.Name == "." {
				continue
			}
			alias = imported.Name.Name
		}
		aliases[alias] = true
	}
	return aliases
}

func matchesCandidateImport(importPath string, expected string, relImport string) bool {
	if expected != "" && importPath == expected {
		return true
	}
	if relImport == "" || relImport == "." {
		return false
	}
	return strings.HasSuffix(importPath, "/"+relImport) || importPath == relImport
}

func goModulePath(root string) string {
	current, err := filepath.Abs(root)
	if err != nil {
		current = root
	}
	for {
		file, err := os.Open(filepath.Join(current, "go.mod"))
		if err == nil {
			scanner := bufio.NewScanner(file)
			for scanner.Scan() {
				fields := strings.Fields(scanner.Text())
				if len(fields) == 2 && fields[0] == "module" {
					file.Close()
					return fields[1]
				}
			}
			file.Close()
		}
		parent := filepath.Dir(current)
		if parent == current {
			return ""
		}
		current = parent
	}
}

func dataTypeKey(path string, packageName string, typeName string) string {
	return filepath.Dir(path) + "|" + packageName + "|" + typeName
}
