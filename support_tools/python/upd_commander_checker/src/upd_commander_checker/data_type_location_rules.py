import ast
from pathlib import Path

from .models import Finding


def check_data_type_locations(paths: list[Path], root: Path) -> list[Finding]:
    trees: dict[Path, ast.AST] = {}
    candidates: list[tuple[Path, int, str, str]] = []

    for path in paths:
        try:
            tree = ast.parse(path.read_text(encoding="utf-8"), filename=str(path))
        except (OSError, UnicodeError, SyntaxError):
            continue
        trees[path] = tree
        classes = [
            node for node in getattr(tree, "body", []) if isinstance(node, ast.ClassDef)
        ]
        if len(classes) < 2:
            continue
        module = _module_name(path, root)
        candidates.extend(
            (path, node.lineno, node.name, module)
            for node in classes
            if _is_data_only(node)
        )

    findings: list[Finding] = []
    for path, line, name, module in candidates:
        external = any(
            other_path != path
            and _references_type(tree, _module_name(other_path, root), module, name)
            for other_path, tree in trees.items()
        )
        relative = _relative(path, root)
        if external:
            findings.append(
                Finding(
                    relative,
                    line,
                    "UPD404",
                    f"data-only type {name} shares a file with another type and is referenced from another file",
                    "warning",
                )
            )
        else:
            findings.append(
                Finding(
                    relative,
                    line,
                    "UPD403",
                    f"data-only type {name} shares a file with another type",
                    "attention",
                )
            )
    return findings


def _is_data_only(class_node: ast.ClassDef) -> bool:
    for node in class_node.body:
        if not isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)):
            continue
        if node.name == "__init__":
            continue
        if node.name.startswith("__") and node.name.endswith("__"):
            continue
        return False
    return True


def _module_name(path: Path, root: Path) -> str:
    relative = _relative(path, root).with_suffix("")
    parts = list(relative.parts)
    if parts and parts[-1] == "__init__":
        parts.pop()
    return ".".join(parts)


def _resolve_from_module(current_module: str, node: ast.ImportFrom) -> str:
    if node.level == 0:
        return node.module or ""
    package = current_module.split(".")[:-1]
    keep = max(0, len(package) - (node.level - 1))
    parts = package[:keep]
    if node.module:
        parts.extend(node.module.split("."))
    return ".".join(parts)


def _attribute_parts(node: ast.AST) -> list[str] | None:
    parts: list[str] = []
    current = node
    while isinstance(current, ast.Attribute):
        parts.append(current.attr)
        current = current.value
    if not isinstance(current, ast.Name):
        return None
    parts.append(current.id)
    parts.reverse()
    return parts


def _has_name_use(tree: ast.AST, name: str) -> bool:
    return any(
        isinstance(node, ast.Name)
        and isinstance(node.ctx, ast.Load)
        and node.id == name
        for node in ast.walk(tree)
    )


def _has_attribute_use(tree: ast.AST, parts: list[str]) -> bool:
    for node in ast.walk(tree):
        if not isinstance(node, ast.Attribute) or not isinstance(node.ctx, ast.Load):
            continue
        if _attribute_parts(node) == parts:
            return True
    return False


def _references_type(
    tree: ast.AST,
    current_module: str,
    candidate_module: str,
    candidate_name: str,
) -> bool:
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            for alias in node.names:
                if alias.name != candidate_module:
                    continue
                if alias.asname:
                    if _has_attribute_use(tree, [alias.asname, candidate_name]):
                        return True
                else:
                    if _has_attribute_use(tree, candidate_module.split(".") + [candidate_name]):
                        return True
        elif isinstance(node, ast.ImportFrom):
            source = _resolve_from_module(current_module, node)
            for alias in node.names:
                if source == candidate_module and alias.name == candidate_name:
                    if _has_name_use(tree, alias.asname or alias.name):
                        return True
                if source == candidate_module and alias.name == "*":
                    if _has_name_use(tree, candidate_name):
                        return True
                module_target = ".".join(part for part in (source, alias.name) if part)
                if module_target == candidate_module:
                    binding = alias.asname or alias.name
                    if _has_attribute_use(tree, [binding, candidate_name]):
                        return True
    return False


def _relative(path: Path, root: Path) -> Path:
    try:
        return path.relative_to(root)
    except ValueError:
        return path
