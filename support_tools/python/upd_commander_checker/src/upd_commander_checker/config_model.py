from dataclasses import dataclass


@dataclass(frozen=True)
class CheckerConfig:
    input_path: str = "."
    output_path: str = ""
    ignore: tuple[str, ...] = ()
    warnings_as_errors: bool = False
    enabled_rules: tuple[str, ...] | None = None
    upd301_max_inputs: int = 2
    flat_layer_min_files: int = 12
    flat_layer_min_direct_percent: int = 80
    model_group_min_items: int = 3
    model_group_min_occurrences: int = 2
