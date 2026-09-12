import json
from pathlib import Path

import PyInstaller.__main__


_DEFAULT_CONFIG = {
    "input": ".",
    "output": "",
    "ignore": [],
    "warnings_as_errors": False,
}


def main() -> None:
    project_root = Path(__file__).resolve().parent.parent
    entry = project_root / "scripts" / "exe_entry.py"
    dist = project_root / "dist"
    PyInstaller.__main__.run(
        [
            str(entry),
            "--onefile",
            "--clean",
            "--name=upd-commander-check",
            f"--paths={project_root / 'src'}",
            f"--distpath={dist}",
            f"--workpath={project_root / 'build'}",
            f"--specpath={project_root / 'build'}",
        ]
    )
    _write_default_config(dist / "config" / "path.json")


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
