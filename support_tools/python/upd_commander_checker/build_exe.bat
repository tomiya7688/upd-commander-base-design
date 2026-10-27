@echo off
setlocal
cd /d "%~dp0"
python scripts\check_build_trust.py
if errorlevel 1 exit /b %errorlevel%
python -m pip install -e ".[build]"
if errorlevel 1 exit /b %errorlevel%
set "PYTHONPATH=%~dp0src;%PYTHONPATH%"
python scripts\build_exe.py
exit /b %errorlevel%
