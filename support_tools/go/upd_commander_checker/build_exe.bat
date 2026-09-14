@echo off
setlocal
cd /d "%~dp0"
if not exist dist mkdir dist
go build -o dist\upd-commander-check.exe .\cmd\upd-commander-check
if errorlevel 1 exit /b %errorlevel%
if not exist dist\config mkdir dist\config
if not exist dist\config\path.json (
  >dist\config\path.json echo {
  >>dist\config\path.json echo   "input": ".",
  >>dist\config\path.json echo   "output": "",
  >>dist\config\path.json echo   "ignore": [],
  >>dist\config\path.json echo   "warnings_as_errors": false,
  >>dist\config\path.json echo   "enabled_rules": ["UPD001", "UPD002", "UPD101", "UPD102", "UPD103", "UPD201", "UPD202", "UPD203", "UPD301", "UPD302", "UPD303", "UPD401", "UPD402", "UPD403", "UPD404"]
  >>dist\config\path.json echo }
)
exit /b 0
