from pathlib import Path

import PyInstaller.__main__


def main() -> None:
    project_root = Path(__file__).resolve().parent.parent
    entry = project_root / "scripts" / "exe_entry.py"
    PyInstaller.__main__.run(
        [
            str(entry),
            "--onefile",
            "--clean",
            "--name=upd-commander-check",
            f"--paths={project_root / 'src'}",
            f"--distpath={project_root / 'dist'}",
            f"--workpath={project_root / 'build'}",
            f"--specpath={project_root / 'build'}",
        ]
    )


if __name__ == "__main__":
    main()
