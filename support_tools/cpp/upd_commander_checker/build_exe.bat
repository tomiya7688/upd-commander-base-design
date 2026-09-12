@echo off
setlocal
cd /d "%~dp0"
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
if errorlevel 1 exit /b %errorlevel%
cmake --build build --config Release --target upd-commander-check
if errorlevel 1 exit /b %errorlevel%
set OUTDIR=build
if exist build\Release\upd-commander-check.exe set OUTDIR=build\Release
if not exist %OUTDIR%\config mkdir %OUTDIR%\config
if not exist %OUTDIR%\config\path.json (
  >%OUTDIR%\config\path.json echo {
  >>%OUTDIR%\config\path.json echo   "input": ".",
  >>%OUTDIR%\config\path.json echo   "output": "",
  >>%OUTDIR%\config\path.json echo   "ignore": [],
  >>%OUTDIR%\config\path.json echo   "warnings_as_errors": false
  >>%OUTDIR%\config\path.json echo }
)
exit /b 0
