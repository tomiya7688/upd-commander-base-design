@echo off
setlocal
cd /d "%~dp0"
python -m pip install -e ".[build]"
if errorlevel 1 exit /b %errorlevel%
set "PYTHONPATH=%~dp0src;%PYTHONPATH%"
python scripts\build_exe.py
exit /b %errorlevel%
