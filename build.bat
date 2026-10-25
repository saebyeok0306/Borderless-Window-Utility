@echo off
rem Builds dist\BorderlessWindowUtility.exe (needs the .NET SDK: https://dotnet.microsoft.com/download)
rem Exit the running program from its tray icon first, or the old .exe cannot be replaced.
cd /d "%~dp0"

dotnet publish BorderlessWindowUtility.csproj -c Release -o dist || exit /b 1

echo.
echo Built dist\BorderlessWindowUtility.exe
