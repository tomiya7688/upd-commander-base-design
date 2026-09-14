import ast

from .models import Finding, ModuleInfo

_COMPONENT_ROLES = {"commander", "messenger", "processing"}


def check_containers(tree: ast.AST, module: ModuleInfo) -> list[Finding]:
    """Check the UPD one-class/one-container boundary rule.

    Compresser modules are intentionally excluded: a Compresser may accept several
    raw values while constructing a class-specific Container, and one Compresser
    may serve several related classes. The invariant being checked is the public
    boundary of UPD component classes, not the internal packing API itself.
    """
    if module.role not in _COMPONENT_ROLES:
        return []

    findings: list[Finding] = []
    for class_node in (node for node in ast.walk(tree) if isinstance(node, ast.ClassDef)):
        for node in class_node.body:
            if not isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)):
                continue
            if node.name.startswith("_"):
                continue

            parameters = _payload_parameters(node.args)
            if len(parameters) > 1:
                findings.append(
                    Finding(
                        module.path,
                        node.lineno,
                        "UPD301",
                        "class operation has multiple inputs; use one Input Container",
                        "warning",
                    )
                )

            if _has_multi_value_return(node):
                findings.append(
                    Finding(
                        module.path,
                        node.lineno,
                        "UPD302",
                        "class operation returns multiple values; use one Output Container",
                        "warning",
                    )
                )
    return findings


def _payload_parameters(arguments: ast.arguments) -> list[ast.arg]:
    positional = list(arguments.posonlyargs) + list(arguments.args)
    if positional and positional[0].arg in {"self", "cls"}:
        positional = positional[1:]
    return positional + list(arguments.kwonlyargs)


def _has_multi_value_return(function: ast.FunctionDef | ast.AsyncFunctionDef) -> bool:
    for node in ast.walk(function):
        if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef, ast.Lambda)) and node is not function:
            continue
        if not isinstance(node, ast.Return) or node.value is None:
            continue
        if isinstance(node.value, (ast.Tuple, ast.List)) and len(node.value.elts) > 1:
            return True
    return False
