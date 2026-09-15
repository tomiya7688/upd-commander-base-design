from pathlib import Path


def finish_report(lines: list[str], output: str | None, exit_code: int) -> int:
    for line in lines:
        print(line)
    if not output:
        return exit_code

    path = Path(output)
    try:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    except (OSError, ValueError):
        print(f"I/O ERROR: failed to write output: {path}")
        return 2
    return exit_code
