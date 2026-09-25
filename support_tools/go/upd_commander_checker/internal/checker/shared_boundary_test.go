package checker

import (
	"path/filepath"
	"strings"
	"testing"
)

func TestCommonSharedDependencyFixtures(t *testing.T) {
	t.Run("all layers may depend on common", func(t *testing.T) {
		for _, source := range []string{
			"ui/messenger/ui_messenger",
			"process/consumer",
			"data/consumer",
		} {
			t.Run(source, func(t *testing.T) {
				findings := scanCommonDependencyFixture(t, source, "common/contracts/message")
				assertNoCode(codeAssertionInput{t: t, findings: findings, code: "UPD101"})
			})
		}
	})

	t.Run("layer to processing-named common contract is allowed", func(t *testing.T) {
		findings := scanCommonDependencyFixture(
			t,
			"ui/messenger/ui_messenger",
			"common/contracts/settings_processing",
		)
		assertNoCode(codeAssertionInput{t: t, findings: findings, code: "UPD101"})
	})

	t.Run("common to each layer is an error", func(t *testing.T) {
		for _, target := range []string{"ui/screen", "process/engine", "data/storage"} {
			t.Run(target, func(t *testing.T) {
				findings := scanCommonDependencyFixture(
					t,
					"common/contracts/bridge",
					target,
				)
				finding := requireFinding(codeAssertionInput{t: t, findings: findings, code: "UPD101"})
				if finding.Severity != "error" {
					t.Fatalf("expected error severity, got %q", finding.Severity)
				}
			})
		}
	})

	t.Run("common messenger to processing-named common contract is allowed", func(t *testing.T) {
		findings := scanCommonDependencyFixture(
			t,
			"common/messenger/common_messenger",
			"shared/contracts/settings_processing",
		)
		assertNoCode(codeAssertionInput{t: t, findings: findings, code: "UPD101"})
	})
}

func requireFinding(input codeAssertionInput) Finding {
	input.t.Helper()
	for _, finding := range input.findings {
		if finding.Code == input.code {
			return finding
		}
	}
	input.t.Fatalf("missing %s: %+v", input.code, input.findings)
	return Finding{}
}

func scanCommonDependencyFixture(t *testing.T, sourcePath, targetPath string) []Finding {
	t.Helper()
	root := t.TempDir()
	sourceFile := filepath.Join(root, filepath.FromSlash(sourcePath+".go"))
	targetFile := filepath.Join(root, filepath.FromSlash(targetPath+"/target.go"))
	importPath := "example/" + strings.TrimSuffix(targetPath, filepath.Base(targetPath)) + filepath.Base(targetPath)
	writeTestFile(t, sourceFile, "package fixture\nimport _ \""+importPath+"\"\n")
	writeTestFile(t, targetFile, "package fixture\n")
	return ScanPath(root, nil)
}

func TestSharedProcessingDoesNotBypassApplicationBoundary(t *testing.T) {
	findings := scanSharedBoundaryTarget(t, "shared/process/settings_processing")
	assertHasCode(codeAssertionInput{t: t, findings: findings, code: "UPD102"})
}

func TestSharedDataDoesNotBypassApplicationBoundary(t *testing.T) {
	findings := scanSharedBoundaryTarget(t, "shared/data/storage")
	assertHasCode(codeAssertionInput{t: t, findings: findings, code: "UPD102"})
}

func TestSharedContractsRemainBoundaryAPI(t *testing.T) {
	findings := scanSharedBoundaryTarget(t, "shared/contracts/process/settings_processing")
	assertNoCode(codeAssertionInput{t: t, findings: findings, code: "UPD102"})
}

func TestSharedMessagesRemainBoundaryAPI(t *testing.T) {
	findings := scanSharedBoundaryTarget(t, "shared/messages/process/settings_processing")
	assertNoCode(codeAssertionInput{t: t, findings: findings, code: "UPD102"})
}

func scanSharedBoundaryTarget(t *testing.T, targetSuffix string) []Finding {
	t.Helper()
	root := t.TempDir()
	targetPackage := filepath.ToSlash(filepath.Join("applications", "settings", targetSuffix))
	source := filepath.Join(root, "applications", "main", "process", "main_commander.go")
	writeTestFile(t, source, "package process\nimport _ \"example/"+targetPackage+"\"\n")
	target := filepath.Join(root, filepath.FromSlash(targetPackage), "target.go")
	writeTestFile(t, target, "package target\n")
	return ScanPath(root, nil)
}
