@echo off
setlocal
cd /d "%~dp0"
dotnet publish UpdCommanderChecker.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist
if errorlevel 1 exit /b %errorlevel%
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\..\copy_third_party_licenses.ps1" -Component roslyn -OutputDirectory "%CD%\dist"
if errorlevel 1 exit /b %errorlevel%
exit /b 0
