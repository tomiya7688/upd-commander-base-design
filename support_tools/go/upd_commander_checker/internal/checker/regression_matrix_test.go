package checker

import (
	"path/filepath"
	"testing"
)

func TestCommanderRulesMatrix(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "process", "rule_commander.go")
	writeTestFile(t, path, "package process\nimport \"os\"\nfunc run() { for i := 0; i < 2; i++ { _ = i + 1 }; _, _ = os.ReadFile(\"sample.txt\") }\n")
	findings := ScanPath(root, nil)
	assertHasCode(t, findings, "UPD201")
	assertHasCode(t, findings, "UPD202")
	assertHasCode(t, findings, "UPD203")
}

func TestContainerRulesMatrix(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "process", "large_commander.go")
	writeTestFile(t, path, "package process\ntype LargeCommander struct{}\nfunc (LargeCommander) Run(\n a int,\n b int,\n c int,\n d int,\n e int,\n f int,\n g int,\n h int,\n i int,\n j int,\n k int,\n l int,\n) (int, int) { return a, b }\n")
	findings := ScanPath(root, nil)
	assertHasCode(t, findings, "UPD301")
	assertHasCode(t, findings, "UPD302")
	assertHasCode(t, findings, "UPD303")
}

func TestCliPathIgnoreMatrix(t *testing.T) {
	root := t.TempDir()
	path := filepath.Join(root, "process", "ignored_commander.go")
	writeTestFile(t, path, "package process\nfunc run() { _ = 1 + 2 }\n")
	findings := ScanPath(root, []string{"process/**"})
	assertNoCode(t, findings, "UPD202")
}
