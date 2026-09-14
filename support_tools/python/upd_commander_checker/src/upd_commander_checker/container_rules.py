import ast

from .models import Finding, ModuleInfo

_BLOAT_ROLES = {"commander", "messenger"}
_MIN_REDUCIBLE_LINES = 10
_MIN_REDUCTION_RATIO = 0.20


def check_containers(tree: ast.AST, module: ModuleInfo) -> list[Finding]:
    """Check the UPD one-class/one-container readability rule.

    Containerization is recommended rather than mandatory because packing and
    unpacking has a cost. Individual unpacked class signatures are therefore
    attention items. Commander/Messenger receives a warning only when replacing
    those signatures with Containers is projected to reduce a meaningful portion
    of the class: at least 10 lines and about 20 percent of the class body.

    Compresser modules are not treated as ordinary class boundaries for warning
    escalation: a Compresser may accept several raw values while constructing
    class-specific Containers, and one Compresser may serve several related classes
    as long as readability is preserved.
    """
    findings: list[Finding] = []
    for class_node in (node for node in ast.walk(tree) if isinstance(node, ast.ClassDef)):
        reducible_lines = 0
        class_lines = _node_lines(class_node)

        for node in class_node.body:
            if not isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)):
                continue
            if node.name.startswith("_"):
                continue

            parameters = _payload_parameters(node.args)
            input_violation = len(parameters) > 1
            if input_violation:
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
            output_violation = returned_values > 1
            if output_violation:
                findings.append(
                    Finding(
                        module.path,
                        node.lineno,
                        "UPD302",
                        "multiple return values reduce readability; consider one Output Container",
                        "attention",
                    )
                )

            if input_violation or output_violation:
                signature_lines = _signature_reducible_lines(node)
                excess_values = max(0, len(parameters) - 1) + max(0, returned_values - 1)
                reducible_lines += max(signature_lines, excess_values)

        if module.role in _BLOAT_ROLES and _large_compression_expected(
            reducible_lines, class_lines
        ):
            findings.append(
                Finding(
                    module.path,
                    class_node.lineno,
                    "UPD303",
                    "Compresser/Container introduction is expected to substantially reduce this Commander/Messenger",
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


def _signature_reducible_lines(function: ast.FunctionDef | ast.AsyncFunctionDef) -> int:
    if not function.body:
        return 0
    signature_lines = max(1, function.body[0].lineno - function.lineno)
    return max(0, signature_lines - 1)


def _node_lines(node: ast.AST) -> int:
    end_line = getattr(node, "end_lineno", getattr(node, "lineno", 1))
    return max(1, end_line - getattr(node, "lineno", 1) + 1)


def _large_compression_expected(reducible_lines: int, class_lines: int) -> bool:
    if reducible_lines < _MIN_REDUCIBLE_LINES:
        return False
    return reducible_lines / max(1, class_lines) >= _MIN_REDUCTION_RATIO
