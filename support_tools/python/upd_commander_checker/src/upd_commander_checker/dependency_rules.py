import ast

from .classifier import classify_import
from .models import Finding, ModuleInfo


def check_dependencies(tree: ast.AST, module: ModuleInfo) -> list[Finding]:
    findings: list[Finding] = []
    for node in ast.walk(tree):
        for imported_name in _imported_names(node):
            target_layer, target_role, target_application = classify_import(imported_name)
            message, code = _dependency_error(
                module,
                target_layer,
                target_role,
                target_application,
                imported_name,
            )
            if message:
                findings.append(
                    Finding(module.path, getattr(node, "lineno", 1), code, message)
                )
    return findings


def _imported_names(node: ast.AST) -> list[str]:
    if isinstance(node, ast.Import):
        return [alias.name for alias in node.names]
    if isinstance(node, ast.ImportFrom) and node.module:
        return [node.module]
    return []


def _dependency_error(
    source: ModuleInfo,
    target_layer: str | None,
    target_role: str | None,
    target_application: str | None,
    imported_name: str,
) -> tuple[str | None, str]:
    if _cross_application_internal_dependency(
        source,
        target_layer,
        target_role,
        target_application,
        imported_name,
    ):
        return "direct dependency on another Application internal module", "UPD102"

    if source.layer == "ui" and target_layer == "data":
        return "UI layer must not depend directly on Data layer", "UPD101"
    if source.layer == "data" and target_layer == "ui":
        return "Data layer must not depend directly on UI layer", "UPD101"
    if source.role == "messenger" and target_role == "processing":
        return "Messenger must not depend directly on Processing", "UPD101"
    if source.role == "processing" and target_role == "processing":
        return "Processing modules must not depend directly on other Processing modules", "UPD101"
    if source.role == "commander" and target_role == "processing":
        if source.layer and target_layer and source.layer != target_layer:
            return "Commander must not depend on Processing in another layer", "UPD101"
    return None, "UPD101"


def _cross_application_internal_dependency(
    source: ModuleInfo,
    target_layer: str | None,
    target_role: str | None,
    target_application: str | None,
    imported_name: str,
) -> bool:
    if not source.application or not target_application:
        return False
    if source.application == target_application:
        return False
    if _is_shared_contract(imported_name):
        return False
    if target_role == "messenger":
        return False
    return target_layer is not None or target_role is not None


def _is_shared_contract(imported_name: str) -> bool:
    parts = imported_name.lower().replace("-", "_").split(".")
    return any(part in {"contract", "contracts", "dto", "dtos", "shared"} for part in parts)
