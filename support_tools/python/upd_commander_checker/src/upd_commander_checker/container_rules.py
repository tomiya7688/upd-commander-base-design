import ast

from .models import Finding, ModuleInfo

_COMPONENT_ROLES = {"commander", "messenger", "processing"}
_BLOAT_ROLES = {"commander", "messenger"}
_BLOAT_OPERATION_THRESHOLD = 3
_BLOAT_EXCESS_SLOT_THRESHOLD = 6


def check_containers(tree: ast.AST, module: ModuleInfo) -> list[Finding]:
    """Check the UPD one-class/one-container readability rule.

    Containerization is recommended rather than mandatory because packing and
    unpacking has a runtime/implementation cost. A single unpacked signature is
    therefore only an attention item. If unpacked signatures accumulate in a
    Commander or Messenger enough to contribute to class bloat, an additional
    warning is emitted.

    Compresser modules are intentionally excluded. A Compresser may accept several
    raw values while constructing class-specific Containers, and one Compresser may
    serve several related classes as long as readability is preserved.
    """
    if module.role not in _COMPONENT_ROLES:
        return []

    findings: list[Finding] = []
    for class_node in (node for node in ast.walk(tree) if isinstance(node, ast.ClassDef)):
        offending_operations = 0
        excess_slots = 0
        first_line = class_node.lineno

        for node in class_node.body:
            if not isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)):
                continue
            if node.name.startswith("_"):
                continue

            operation_offends = False
            parameters = _payload_parameters(node.args)
            if len(parameters) > 1:
                operation_offends = True
                excess_slots += len(parameters) - 1
                findings.append(
                    Finding(
                        module.path,
                        node.lineno,
                        "UPD301",
                        "multiple inputs reduce readability; consider one Input Container",
                        "attention",
                    )
                )

            returned_values = _max_multi_value_return(node)
            if returned_values > 1:
                operation_offends = True
                excess_slots += returned_values - 1
                findings.append(
                    Finding(
                        module.path,
                        node.lineno,
                        "UPD302",
                        "multiple return values reduce readability; consider one Output Container",
                        "attention",
                    )
                )

            if operation_offends:
                offending_operations += 1

        if (
            module.role in _BLOAT_ROLES
            and (
                offending_operations >= _BLOAT_OPERATION_THRESHOLD
                or excess_slots >= _BLOAT_EXCESS_SLOT_THRESHOLD
            )
        ):
            findings.append(
                Finding(
                    module.path,
                    first_line,
                    "UPD303",
                    "uncontainerized signatures contribute to Commander/Messenger bloat",
                    "warning",
                )
            )
    return findings


def _payload_parameters(arguments: ast.arguments) -> list[ast.arg]:
    positional = list(arguments.posonlyargs) + list(arguments.args)
    if positional and positional[0].arg in {"self", "cls"}:
        positional = positional[1:]
    return positional + list(arguments.kwonlyargs)


def _max_multi_value_return(function: ast.FunctionDef | ast.AsyncFunctionDef) -> int:
    maximum = 0
    for node in ast.walk(function):
        if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef, ast.Lambda)) and node is not function:
            continue
        if not isinstance(node, ast.Return) or node.value is None:
            continue
        if isinstance(node.value, (ast.Tuple, ast.List)):
            maximum = max(maximum, len(node.value.elts))
    return maximum
