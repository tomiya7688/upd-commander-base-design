import ast
from pathlib import Path

from .models import Finding


def check_data_type_locations(paths: list[Path], root: Path) -> list[Finding]:
    trees: dict[Path, ast.AST] = {}
    candidates: list[tuple[Path, int, str]] = []

    for path in paths:
        try:
            tree = ast.parse(path.read_text(encoding="utf-8"), filename=str(path))
        except (OSError, UnicodeError, SyntaxError):
            continue
        trees[path] = tree
        data_types = [
            node
            for node in getattr(tree, "body", [])
            if isinstance(node, ast.ClassDef) and _is_data_only(node)
        ]
        if len(data_types) > 1:
            candidates.extend((path, node.lineno, node.name) for node in data_types)

    findings: list[Finding] = []
    for path, line, name in candidates:
        external = any(
            other_path != path and _references_name(tree, name)
            for other_path, tree in trees.items()
        )
        relative = _relative(path, root)
        if external:
            findings.append(
                Finding(
                    relative,
                    line,
                    "UPD404",
                    f"data-only type {name} shares a file and is referenced from another file",
                    "warning",
                )
            )
        else:
            findings.append(
                Finding(
                    relative,
                    line,
                    "UPD403",
                    f"multiple data-only types share this file; {name} is local-only",
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


def _references_name(tree: ast.AST, name: str) -> bool:
    for node in ast.walk(tree):
        if isinstance(node, ast.Name) and node.id == name:
            return True
        if isinstance(node, ast.Attribute) and node.attr == name:
            return True
        if isinstance(node, ast.alias) and node.name.split(".")[-1] == name:
            return True
    return False


def _relative(path: Path, root: Path) -> Path:
    try:
        return path.relative_to(root)
    except ValueError:
        return path
