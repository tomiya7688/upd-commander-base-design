@echo off
setlocal
cd /d "%~dp0"
dotnet publish UpdCommanderChecker.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist
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
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\..\copy_third_party_licenses.ps1" -Component roslyn -OutputDirectory "%CD%\dist"
if errorlevel 1 exit /b %errorlevel%
exit /b 0
