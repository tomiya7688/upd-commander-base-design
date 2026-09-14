import ast

from .models import Finding, ModuleInfo

_MAX_CLASS_LINES = 250
_MAX_CLASS_METHODS = 12


def check_responsibilities(tree: ast.AST, module: ModuleInfo) -> list[Finding]:
    findings: list[Finding] = []
    classes = [node for node in getattr(tree, "body", []) if isinstance(node, ast.ClassDef)]

    major_classes = [node for node in classes if _has_behavior(node)]
    if len(major_classes) > 1:
        findings.append(
            Finding(
                module.path,
                major_classes[1].lineno,
                "UPD402",
                "file contains multiple responsibility-bearing classes",
                "warning",
            )
        )

    for class_node in classes:
        method_count = sum(
            isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef))
            and node.name != "__init__"
            for node in class_node.body
        )
        end_line = getattr(class_node, "end_lineno", class_node.lineno)
        line_count = max(1, end_line - class_node.lineno + 1)
        if line_count > _MAX_CLASS_LINES or method_count > _MAX_CLASS_METHODS:
            findings.append(
                Finding(
                    module.path,
                    class_node.lineno,
                    "UPD401",
                    f"class {class_node.name} is too large for one responsibility "
                    f"(lines={line_count}, methods={method_count})",
                    "warning",
                )
            )

    if not classes:
        end_line = max(
            (getattr(node, "end_lineno", getattr(node, "lineno", 1)) for node in getattr(tree, "body", [])),
            default=1,
        )
        if end_line > _MAX_CLASS_LINES:
            findings.append(
                Finding(
                    module.path,
                    1,
                    "UPD401",
                    "file/module approximation is too large for one responsibility",
                    "warning",
                )
            )

    return findings


def _has_behavior(class_node: ast.ClassDef) -> bool:
    return any(
        isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef))
        and not (node.name.startswith("__") and node.name.endswith("__"))
        for node in class_node.body
    )
