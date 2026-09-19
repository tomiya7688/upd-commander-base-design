import ast
from fnmatch import fnmatch
from pathlib import Path
import re

from .classifier import classify_module
from .ignore_rules import IgnoreRule
from .models import Finding, ModuleInfo


_INLINE_IGNORE = re.compile(r"#\s*upd:\s*ignore\s+(UPD\d+|all)\b", re.IGNORECASE)
ModelGroupOccurrence = tuple[str, int, str, str, str, tuple[str, ...]]


def collect_path_model_group_occurrences(
    path: Path,
    root: Path,
    classification_root: Path | None,
    ignore_rules: tuple[IgnoreRule, ...],
    min_items: int,
) -> list[ModelGroupOccurrence]:
    try:
        source = path.read_text(encoding="utf-8")
        tree = ast.parse(source, filename=str(path))
    except (OSError, UnicodeError, SyntaxError):
        return []
    module = classify_module(path, classification_root)
    relative = _relative_text(path, root)
    return collect_model_group_occurrences(
        tree, module, source, relative, ignore_rules, min_items
    )


def collect_model_group_occurrences(
    tree: ast.AST,
    module: ModuleInfo,
    source: str,
    relative_path: str,
    ignore_rules: tuple[IgnoreRule, ...],
    min_items: int,
) -> list[ModelGroupOccurrence]:
    collector = _Collector(module, source, relative_path, ignore_rules, min_items)
    collector.visit(tree)
    return collector.occurrences


def model_attention_findings(
    occurrences: list[ModelGroupOccurrence],
    min_occurrences: int,
) -> list[Finding]:
    grouped: dict[str, list[ModelGroupOccurrence]] = {}
    for occurrence in occurrences:
        grouped.setdefault(_signature(occurrence), []).append(occurrence)

    findings: list[Finding] = []
    for group in grouped.values():
        if len(group) < min_occurrences:
            continue
        first = min(group, key=lambda item: (item[0], item[1]))
        items = ",".join(first[5])
        findings.append(
            Finding(
                Path(first[0]),
                first[1],
                "UPD406",
                f"repeated value group may benefit from a Model/DTO; items={items} occurrences={len(group)} kind={first[4]}",
                "attention",
            )
        )
    return findings


class _Collector(ast.NodeVisitor):
    def __init__(
        self,
        module: ModuleInfo,
        source: str,
        relative_path: str,
        ignore_rules: tuple[IgnoreRule, ...],
        min_items: int,
    ) -> None:
        self.module = module
        self.lines = source.splitlines()
        self.relative_path = relative_path
        self.ignore_rules = ignore_rules
        self.min_items = min_items
        self.occurrences: list[ModelGroupOccurrence] = []

    def visit_FunctionDef(self, node: ast.FunctionDef) -> None:
        self._parameter_group(node)
        self.generic_visit(node)

    def visit_AsyncFunctionDef(self, node: ast.AsyncFunctionDef) -> None:
        self._parameter_group(node)
        self.generic_visit(node)

    def visit_Assign(self, node: ast.Assign) -> None:
        if isinstance(node.value, ast.Tuple):
            self._tuple_group(node.value)
        self._parallel_group(node)
        self.generic_visit(node)

    def visit_AnnAssign(self, node: ast.AnnAssign) -> None:
        if isinstance(node.value, ast.Tuple):
            self._tuple_group(node.value)
        self._parallel_group(node)
        self.generic_visit(node)

    def visit_Expr(self, node: ast.Expr) -> None:
        self._parallel_group(node)
        self.generic_visit(node)

    def visit_Return(self, node: ast.Return) -> None:
        if isinstance(node.value, ast.Tuple):
            self._tuple_group(node.value)
        self._parallel_group(node)
        self.generic_visit(node)

    def _tuple_group(self, node: ast.Tuple) -> None:
        items = tuple(filter(None, (_item_key(item) for item in node.elts)))
        if len(items) == len(node.elts):
            self._add((node.lineno, "tuple", items))

    def _parameter_group(self, node: ast.FunctionDef | ast.AsyncFunctionDef) -> None:
        positional = list(node.args.posonlyargs) + list(node.args.args)
        if positional and positional[0].arg in {"self", "cls"} and not _has_staticmethod(node):
            positional = positional[1:]
        parameters = positional + list(node.args.kwonlyargs)
        if node.args.vararg is not None:
            parameters.append(node.args.vararg)
        if node.args.kwarg is not None:
            parameters.append(node.args.kwarg)
        items = tuple(_normalize(item.arg) for item in parameters)
        self._add((node.lineno, "parameters", items))

    def _parallel_group(self, node: ast.AST) -> None:
        groups: dict[str, list[str]] = {}
        for child in ast.walk(node):
            if not isinstance(child, ast.Subscript):
                continue
            collection = _item_key(child.value)
            index = _index_key(child.slice)
            if collection and index:
                groups.setdefault(index, []).append(collection)
        for items in groups.values():
            ordered = tuple(dict.fromkeys(items))
            self._add((getattr(node, "lineno", 1), "parallel_collection", ordered))

    def _add(self, occurrence: tuple[int, str, tuple[str, ...]]) -> None:
        line, kind, items = occurrence
        if len(items) < self.min_items or len(set(items)) != len(items):
            return
        if self._ignored(line):
            return
        self.occurrences.append(
            (
                self.relative_path,
                line,
                self.module.application or "",
                self.module.layer or "",
                kind,
                items,
            )
        )

    def _ignored(self, line: int) -> bool:
        if any(
            rule.code == "UPD406" and fnmatch(self.relative_path, rule.pattern)
            for rule in self.ignore_rules
        ):
            return True
        if not 1 <= line <= len(self.lines):
            return False
        match = _INLINE_IGNORE.search(self.lines[line - 1])
        return bool(match and match.group(1).upper() in {"ALL", "UPD406"})


def _signature(occurrence: ModelGroupOccurrence) -> str:
    return "\x1f".join(
        (occurrence[2], occurrence[3], occurrence[4], "\x1e".join(occurrence[5]))
    )


def _item_key(node: ast.AST) -> str:
    if isinstance(node, ast.Name):
        return _normalize(node.id)
    if isinstance(node, ast.Attribute):
        return _normalize(node.attr)
    if isinstance(node, ast.Subscript):
        return _item_key(node.value)
    return ""


def _index_key(node: ast.AST) -> str:
    if isinstance(node, ast.Name):
        return "name:" + _normalize(node.id)
    if isinstance(node, ast.Constant):
        return "const:" + repr(node.value)
    return ast.dump(node, annotate_fields=False, include_attributes=False)


def _normalize(value: str) -> str:
    return value.lstrip("_").lower()


def _has_staticmethod(node: ast.FunctionDef | ast.AsyncFunctionDef) -> bool:
    return any(
        isinstance(item, ast.Name) and item.id == "staticmethod"
        or isinstance(item, ast.Attribute) and item.attr == "staticmethod"
        for item in node.decorator_list
    )


def _relative_text(path: Path, root: Path) -> str:
    try:
        return path.relative_to(root).as_posix()
    except ValueError:
        return path.as_posix()
