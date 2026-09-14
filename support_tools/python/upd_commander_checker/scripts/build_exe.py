import json
import sys
from pathlib import Path

import PyInstaller.__main__

_PROJECT_ROOT = Path(__file__).resolve().parent.parent
_SOURCE_PATH = _PROJECT_ROOT / "src"
sys.path.insert(0, str(_SOURCE_PATH))

from upd_commander_checker.rule_selection import SUPPORTED_RULES  # noqa: E402


_DEFAULT_CONFIG = {
    "input": ".",
    "output": "",
    "ignore": [],
    "warnings_as_errors": False,
    "enabled_rules": list(SUPPORTED_RULES),
}


def main() -> None:
    project_root = _PROJECT_ROOT
    source_path = _SOURCE_PATH
    dist = project_root / "dist"
    bundle = dist / "upd-commander-check"
    work = project_root / "build"

    PyInstaller.__main__.run(
        [
            str(project_root / "scripts" / "exe_entry.py"),
            "--onedir",
            "--clean",
            "--noconfirm",
            "--name=upd-commander-check",
            f"--paths={source_path}",
            f"--distpath={dist}",
            f"--workpath={work / 'cui'}",
            f"--specpath={work}",
        ]
    )
    PyInstaller.__main__.run(
        [
            str(project_root / "scripts" / "gui_entry.py"),
            "--onefile",
            "--windowed",
            "--clean",
            "--noconfirm",
            "--name=upd-commander-check-gui",
            f"--paths={source_path}",
            f"--distpath={bundle}",
            f"--workpath={work / 'gui'}",
            f"--specpath={work}",
        ]
    )
    _write_default_config(bundle / "config" / "path.json")


def _write_default_config(path: Path) -> None:
    if path.exists():
        return
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        json.dumps(_DEFAULT_CONFIG, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
