@echo off
setlocal
cd /d "%~dp0"
set "PYTHONPATH=%~dp0src;%PYTHONPATH%"
python -m unittest discover -s tests -p "test_*.py"
if errorlevel 1 exit /b %errorlevel%
call build_exe.bat
if errorlevel 1 exit /b %errorlevel%
set "BUNDLE=dist\upd-commander-check"
if not exist "%BUNDLE%\upd-commander-check.exe" exit /b 2
if not exist "%BUNDLE%\upd-commander-check-gui.exe" exit /b 3
if not exist "%BUNDLE%\config\path.json" exit /b 4
findstr /c:"enabled_rules" "%BUNDLE%\config\path.json" >nul || exit /b 5
"%BUNDLE%\upd-commander-check.exe" . --warnings-as-errors --attentions-as-errors
if errorlevel 1 exit /b %errorlevel%
powershell -NoProfile -ExecutionPolicy Bypass -Command "$p=Start-Process -FilePath (Resolve-Path '%BUNDLE%\upd-commander-check-gui.exe') -WindowStyle Hidden -PassThru; Start-Sleep -Seconds 3; $p.Refresh(); if($p.HasExited){exit 1}; Stop-Process -Id $p.Id"
if errorlevel 1 exit /b %errorlevel%
echo ACCEPTANCE OK
exit /b 0