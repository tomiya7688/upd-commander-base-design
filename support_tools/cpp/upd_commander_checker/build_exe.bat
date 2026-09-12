@echo off
setlocal
cd /d "%~dp0"
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
if errorlevel 1 exit /b %errorlevel%
cmake --build build --config Release --target upd-commander-check
exit /b %errorlevel%
