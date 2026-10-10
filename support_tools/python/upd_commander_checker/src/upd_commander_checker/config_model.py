from dataclasses import dataclass

from .gate_exception import GateException


@dataclass(frozen=True)
class CheckerConfig:
    input_path: str = "."
    output_path: str = ""
    ignore: tuple[str, ...] = ()
    warnings_as_errors: bool = False
    fail_on: tuple[str, ...] | None = None
    fail_on_scope: str = "all"
    severity_overrides: tuple[tuple[str, str], ...] = ()
    gate_exceptions: tuple[GateException, ...] = ()
    common_roots: tuple[str, ...] = ("common", "shared")
    enabled_rules: tuple[str, ...] | None = None
    upd301_max_inputs: int = 2
    flat_layer_min_files: int = 12
    flat_layer_min_direct_percent: int = 80
    model_group_min_items: int = 3
    model_group_min_occurrences: int = 2
