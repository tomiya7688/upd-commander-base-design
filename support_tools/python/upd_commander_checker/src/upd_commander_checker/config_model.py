from dataclasses import dataclass


@dataclass(frozen=True)
class CheckerConfig:
    input_path: str = "."
    output_path: str = ""
    ignore: tuple[str, ...] = ()
    warnings_as_errors: bool = False
    enabled_rules: tuple[str, ...] | None = None
