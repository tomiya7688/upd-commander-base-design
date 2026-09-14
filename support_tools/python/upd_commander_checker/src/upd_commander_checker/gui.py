import json
import os
import queue
import subprocess
import sys
import threading
from pathlib import Path

from .config import ConfigError, config_path_for_write, load_config
from .rule_selection import SUPPORTED_RULES


def main() -> int:
    try:
        import tkinter as tk
        from tkinter import filedialog, messagebox, ttk
    except ImportError as exc:
        print(f"GUI ERROR: tkinter is unavailable: {exc}")
        return 2

    try:
        config = load_config()
    except ConfigError as exc:
        messagebox.showerror("UPD Commander Checker", f"CONFIG ERROR: {exc}")
        return 2

    root = tk.Tk()
    root.title("UPD Commander Checker")
    root.geometry("920x720")
    root.minsize(760, 560)

    target = tk.StringVar(value=config.input_path)
    output = tk.StringVar(value=config.output_path)
    warnings_as_errors = tk.BooleanVar(value=config.warnings_as_errors)
    selected = set(
        config.enabled_rules if config.enabled_rules is not None else SUPPORTED_RULES
    )
    rule_values = {code: tk.BooleanVar(value=code in selected) for code in SUPPORTED_RULES}

    frame = ttk.Frame(root, padding=12)
    frame.pack(fill="both", expand=True)
    frame.columnconfigure(1, weight=1)
    frame.rowconfigure(6, weight=1)

    ttk.Label(frame, text="チェック対象").grid(row=0, column=0, sticky="w")
    ttk.Entry(frame, textvariable=target).grid(row=0, column=1, sticky="ew", padx=8)

    def choose_target() -> None:
        selected_path = filedialog.askdirectory(initialdir=target.get() or str(Path.cwd()))
        if selected_path:
            target.set(selected_path)

    ttk.Button(frame, text="参照", command=choose_target).grid(row=0, column=2)
    ttk.Label(frame, text="結果出力（任意）").grid(row=1, column=0, sticky="w", pady=(8, 0))
    ttk.Entry(frame, textvariable=output).grid(row=1, column=1, sticky="ew", padx=8, pady=(8, 0))
    ttk.Checkbutton(
        frame,
        text="Warningをエラー扱い",
        variable=warnings_as_errors,
    ).grid(row=2, column=0, columnspan=3, sticky="w", pady=(8, 0))

    ttk.Label(frame, text="有効にするUPDルール").grid(
        row=3, column=0, columnspan=3, sticky="w", pady=(12, 4)
    )
    rules_frame = ttk.Frame(frame)
    rules_frame.grid(row=4, column=0, columnspan=3, sticky="ew")
    for index, code in enumerate(SUPPORTED_RULES):
        ttk.Checkbutton(rules_frame, text=code, variable=rule_values[code]).grid(
            row=index // 5, column=index % 5, sticky="w", padx=(0, 18), pady=2
        )

    actions = ttk.Frame(frame)
    actions.grid(row=5, column=0, columnspan=3, sticky="ew", pady=12)
    status = ttk.Label(actions, text="準備完了")
    status.pack(side="right")

    result = tk.Text(frame, wrap="none", font=("Consolas", 10))
    result.grid(row=6, column=0, columnspan=3, sticky="nsew")

    def settings_data() -> dict[str, object]:
        path = config_path_for_write()
        base = path.parent.parent
        return {
            "input": _relative_path(base, target.get().strip() or "."),
            "output": _relative_path(base, output.get().strip()) if output.get().strip() else "",
            "ignore": list(config.ignore),
            "warnings_as_errors": warnings_as_errors.get(),
            "enabled_rules": [code for code in SUPPORTED_RULES if rule_values[code].get()],
        }

    def save_settings() -> bool:
        try:
            path = config_path_for_write()
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(
                json.dumps(settings_data(), indent=2, ensure_ascii=False) + "\n",
                encoding="utf-8",
            )
            status.configure(text=f"設定保存: {path}")
            return True
        except OSError as exc:
            messagebox.showerror("UPD Commander Checker", f"設定を保存できません: {exc}")
            return False

    run_results: queue.Queue[subprocess.CompletedProcess[str] | OSError] = queue.Queue()

    def finish_run(completed: subprocess.CompletedProcess[str] | OSError) -> None:
        result.delete("1.0", "end")
        if isinstance(completed, OSError):
            result.insert("1.0", f"GUI ERROR: {completed}\n")
            status.configure(text="実行失敗")
        else:
            result.insert("1.0", completed.stdout or completed.stderr or "(出力なし)\n")
            status.configure(text=f"終了コード: {completed.returncode}")
        run_button.configure(state="normal")

    def poll_run() -> None:
        try:
            completed = run_results.get_nowait()
        except queue.Empty:
            root.after(100, poll_run)
            return
        finish_run(completed)

    def run_worker(command: list[str]) -> None:
        try:
            run_results.put(subprocess.run(
                command,
                capture_output=True,
                text=True,
                encoding="utf-8",
                errors="replace",
                check=False,
            ))
        except OSError as exc:
            run_results.put(exc)

    def run_check() -> None:
        if not save_settings():
            return
        run_button.configure(state="disabled")
        status.configure(text="チェック中...")
        result.delete("1.0", "end")
        threading.Thread(target=run_worker, args=(_checker_command(),), daemon=True).start()
        root.after(100, poll_run)

    ttk.Button(actions, text="設定を保存", command=save_settings).pack(side="left")
    run_button = ttk.Button(actions, text="チェック実行", command=run_check)
    run_button.pack(side="left", padx=8)
    root.mainloop()
    return 0


def _checker_command() -> list[str]:
    if getattr(sys, "frozen", False):
        executable = Path(sys.executable).resolve().parent / "upd-commander-check.exe"
        return [str(executable)]
    return [sys.executable, "-m", "upd_commander_checker"]


def _relative_path(base: Path, value: str) -> str:
    path = Path(value).expanduser()
    if not path.is_absolute():
        path = (base / path).resolve()
    try:
        return os.path.relpath(path, base)
    except ValueError:
        return str(path)


if __name__ == "__main__":
    raise SystemExit(main())
