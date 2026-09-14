package checker

import (
	"go/ast"
	"go/parser"
	"go/token"
	"path/filepath"
)

type parsedDataFile struct {
	path    string
	rel     string
	file    *ast.File
	fset    *token.FileSet
	packageName string
}

type dataOnlyType struct {
	path string
	rel  string
	name string
	line int
}

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

	byFile := map[string][]dataOnlyType{}
	for _, item := range parsed {
		for _, declaration := range item.file.Decls {
			gen, ok := declaration.(*ast.GenDecl)
			if !ok || gen.Tok != token.TYPE {
				continue
			}
			for _, spec := range gen.Specs {
				typeSpec, ok := spec.(*ast.TypeSpec)
				if !ok {
					continue
				}
				if _, ok := typeSpec.Type.(*ast.StructType); !ok {
					continue
				}
				if methodCounts[dataTypeKey(item.path, item.packageName, typeSpec.Name.Name)] != 0 {
					continue
				}
				byFile[item.path] = append(byFile[item.path], dataOnlyType{
					path: item.path,
					rel: item.rel,
					name: typeSpec.Name.Name,
					line: item.fset.Position(typeSpec.Pos()).Line,
				})
			}
		}
	}

	var findings []Finding
	for _, group := range byFile {
		if len(group) < 2 {
			continue
		}
		for _, item := range group {
			code := "UPD403"
			severity := "attention"
			message := "multiple data-only types share this file; " + item.name + " is local-only"
			if dataTypeReferencedElsewhere(item, parsed) {
				code = "UPD404"
				severity = "warning"
				message = "data-only type " + item.name + " shares a file and is referenced from another file"
			}
			addFinding(&findings, item.rel, item.line, code, message, severity, readLines(item.path), rules)
		}
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

func dataTypeReferencedElsewhere(item dataOnlyType, files []parsedDataFile) bool {
	for _, other := range files {
		if other.path == item.path {
			continue
		}
		found := false
		ast.Inspect(other.file, func(node ast.Node) bool {
			identifier, ok := node.(*ast.Ident)
			if ok && identifier.Name == item.name {
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

func dataTypeKey(path string, packageName string, typeName string) string {
	return filepath.Dir(path) + "|" + packageName + "|" + typeName
}
