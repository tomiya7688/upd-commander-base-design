package main

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"
)

func finishReport(lines []string, output string, code int) int {
	for _, line := range lines {
		fmt.Println(line)
	}
	if output == "" {
		return code
	}
	if dir := filepath.Dir(output); dir != "." {
		if err := os.MkdirAll(dir, 0o755); err != nil {
			fmt.Printf("I/O ERROR: failed to write output: %s\n", output)
			return 2
		}
	}
	if err := os.WriteFile(output, []byte(strings.Join(lines, "\n")+"\n"), 0o644); err != nil {
		fmt.Printf("I/O ERROR: failed to write output: %s\n", output)
		return 2
	}
	return code
}
