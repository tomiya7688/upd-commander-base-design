@echo off
setlocal
cd /d "%~dp0"
if not exist dist mkdir dist
go build -o dist\upd-commander-check.exe .\cmd\upd-commander-check
exit /b %errorlevel%
