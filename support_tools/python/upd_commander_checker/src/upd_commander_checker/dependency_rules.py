import ast

from .classifier import classify_import
from .models import DependencyRuleResult, Finding, ModuleInfo


def check_dependencies(tree: ast.AST, module: ModuleInfo) -> list[Finding]:
    findings: list[Finding] = []
    for node in ast.walk(tree):
        for imported_name in _imported_names(node):
            target_layer, target_role, target_application = classify_import(imported_name)
            result = _dependency_result(
                module,
                target_layer,
                target_role,
                target_application,
                imported_name,
            )
            if result:
                findings.append(
                    Finding(
                        module.path,
                        getattr(node, "lineno", 1),
                        result.code,
                        result.message,
                        result.severity,
                    )
                )
    return findings


def _imported_names(node: ast.AST) -> list[str]:
    if isinstance(node, ast.Import):
        return [alias.name for alias in node.names]
    if isinstance(node, ast.ImportFrom) and node.module:
        return [node.module]
    return []


def _dependency_result(
    source: ModuleInfo,
    target_layer: str | None,
    target_role: str | None,
    target_application: str | None,
    imported_name: str,
) -> DependencyRuleResult | None:
    if _cross_application_internal_dependency(
        source,
        target_layer,
        target_role,
        target_application,
        imported_name,
    ):
        return DependencyRuleResult(
            "UPD102",
            "direct dependency on another Application internal module",
            "error",
        )

    if source.layer == "ui" and target_layer == "data":
        return DependencyRuleResult("UPD101", "UI layer must not depend directly on Data layer", "error")
    if source.layer == "data" and target_layer == "ui":
        return DependencyRuleResult("UPD101", "Data layer must not depend directly on UI layer", "error")
    if source.role == "messenger" and target_role == "processing":
        return DependencyRuleResult("UPD101", "Messenger must not depend directly on Processing", "error")
    if source.role == "processing" and target_role == "processing":
        return DependencyRuleResult(
            "UPD101",
            "Processing modules must not depend directly on other Processing modules",
            "error",
        )
    if source.role == "commander" and target_role == "processing":
        if source.layer and target_layer and source.layer != target_layer:
            return DependencyRuleResult(
                "UPD101",
                "Commander must not depend on Processing in another layer",
                "error",
            )
    if (
        source.layer == "data"
        and source.role == "commander"
        and target_layer == "data"
        and target_role == "commander"
    ):
        return DependencyRuleResult(
            "UPD103",
            "Data Commander should not communicate directly with another Data Commander",
            "warning",
        )
    return None


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
