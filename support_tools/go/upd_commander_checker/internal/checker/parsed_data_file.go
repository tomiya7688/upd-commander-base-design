package checker

import (
	"go/ast"
	"go/token"
)

type parsedDataFile struct {
	path        string
	rel         string
	file        *ast.File
	fset        *token.FileSet
	packageName string
}
