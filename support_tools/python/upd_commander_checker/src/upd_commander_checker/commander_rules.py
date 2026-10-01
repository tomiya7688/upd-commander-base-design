import ast

from .models import Finding, ModuleInfo


_EXACT_DIRECT_CALLS = {
    "builtins.open",
    "json.dumps",
    "json.loads",
}
_DIRECT_CALL_PREFIXES = (
    "sqlite3.",
    "pygame.",
)


def check_commander(tree: ast.AST, module: ModuleInfo) -> list[Finding]:
    if module.role != "commander":
        return []

    imports = _import_aliases(tree)
    module_definitions = _module_definitions(tree)
    parents = {
        child: parent for parent in ast.walk(tree) for child in ast.iter_child_nodes(parent)
    }
    findings: list[Finding] = []
    for node in ast.walk(tree):
        if isinstance(node, (ast.For, ast.While)):
            if isinstance(node, ast.For):
                context = (
                    "for:target="
                    + ast.dump(node.target, include_attributes=False)
                    + ":iter="
                    + ast.dump(node.iter, include_attributes=False)
                )
            else:
                context = "while:test=" + ast.dump(node.test, include_attributes=False)
            findings.append(
                Finding(
                    module.path,
                    node.lineno,
                    "UPD201",
                    "Commander contains a loop; verify that real processing has not leaked into it",
                    "warning",
                    symbol=_enclosing_symbol(node, parents),
                    context=context,
                )
            )
        if isinstance(node, ast.BinOp):
            findings.append(
                Finding(
                    module.path,
                    node.lineno,
                    "UPD202",
                    "Commander contains a calculation expression",
                    "warning",
                    symbol=_enclosing_symbol(node, parents),
                    context="expression:" + ast.dump(node, include_attributes=False),
                )
            )
        if isinstance(node, ast.Call):
            call_name = _resolved_call_name(node.func, imports, module_definitions)
            if _is_direct_work_call(call_name):
                findings.append(
                    Finding(
                        module.path,
                        node.lineno,
                        "UPD203",
                        f"Commander should not perform direct processing/API work: {call_name}",
                        symbol=_enclosing_symbol(node, parents),
                        context="direct-work-call:" + call_name,
                    )
                )
    return findings


def _enclosing_symbol(node: ast.AST, parents: dict[ast.AST, ast.AST]) -> str:
    current = parents.get(node)
    names: list[str] = []
    while current is not None:
        if isinstance(current, (ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef)):
            names.append(current.name)
        current = parents.get(current)
    return ".".join(reversed(names))


def _import_aliases(tree: ast.AST) -> dict[str, str]:
    aliases: dict[str, str] = {}
    for node in getattr(tree, "body", []):
        if isinstance(node, ast.Import):
            for imported in node.names:
                local_name = imported.asname or imported.name.split(".", 1)[0]
                target = imported.name if imported.asname else local_name
                aliases[local_name] = target
        elif isinstance(node, ast.ImportFrom) and node.module:
            for imported in node.names:
                if imported.name == "*":
                    continue
                local_name = imported.asname or imported.name
                aliases[local_name] = f"{node.module}.{imported.name}"
    return aliases


def _module_definitions(tree: ast.AST) -> set[str]:
    names: set[str] = set()
    for node in getattr(tree, "body", []):
        if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef)):
            names.add(node.name)
        elif isinstance(node, (ast.Assign, ast.AnnAssign)):
            targets = node.targets if isinstance(node, ast.Assign) else [node.target]
            for target in targets:
                if isinstance(target, ast.Name):
                    names.add(target.id)
    return names


def _resolved_call_name(
    node: ast.AST,
    imports: dict[str, str],
    module_definitions: set[str],
) -> str:
    raw_name = _call_name(node)
    if not raw_name:
        return ""
    root, separator, suffix = raw_name.partition(".")
    if root in imports:
        resolved_root = imports[root]
        return f"{resolved_root}.{suffix}" if separator else resolved_root
    if not separator and root == "open" and root not in module_definitions:
        return "builtins.open"
    return raw_name


def _is_direct_work_call(call_name: str) -> bool:
    return call_name in _EXACT_DIRECT_CALLS or any(
        call_name.startswith(prefix) for prefix in _DIRECT_CALL_PREFIXES
    )


def _call_name(node: ast.AST) -> str:
    if isinstance(node, ast.Name):
        return node.id
    if isinstance(node, ast.Attribute):
        prefix = _call_name(node.value)
        return f"{prefix}.{node.attr}" if prefix else node.attr
    return ""
