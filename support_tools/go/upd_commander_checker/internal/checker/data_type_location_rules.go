package checker

import (
	"go/ast"
	"go/parser"
	"go/token"
	"path/filepath"
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

func dataTypeKey(path string, packageName string, typeName string) string {
	return filepath.Dir(path) + "|" + packageName + "|" + typeName
}
