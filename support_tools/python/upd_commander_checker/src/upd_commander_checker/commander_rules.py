import ast

from .models import Finding, ModuleInfo


_FORBIDDEN_CALL_FRAGMENTS = {
    "open",
    "json.dumps",
    "json.loads",
    "sqlite3",
    "pygame",
}


def check_commander(tree: ast.AST, module: ModuleInfo) -> list[Finding]:
    if module.role != "commander":
        return []

    findings: list[Finding] = []
    for node in ast.walk(tree):
        if isinstance(node, (ast.For, ast.While)):
            findings.append(
                Finding(module.path, node.lineno, "UPD201", "Commander contains a loop; verify that real processing has not leaked into it", "warning")
            )
        if isinstance(node, ast.BinOp):
            findings.append(
                Finding(module.path, node.lineno, "UPD202", "Commander contains a calculation expression", "warning")
            )
        if isinstance(node, ast.Call):
            call_name = _call_name(node.func)
            if any(fragment in call_name for fragment in _FORBIDDEN_CALL_FRAGMENTS):
                findings.append(
                    Finding(module.path, node.lineno, "UPD203", f"Commander should not perform direct processing/API work: {call_name}")
                )
    return findings


def _call_name(node: ast.AST) -> str:
    if isinstance(node, ast.Name):
        return node.id
    if isinstance(node, ast.Attribute):
        prefix = _call_name(node.value)
        return f"{prefix}.{node.attr}" if prefix else node.attr
    return ""
