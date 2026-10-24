@echo off
setlocal EnableExtensions EnableDelayedExpansion

rem ====================================================================
rem  RDOnline (RDOL) - Windows package builder
rem
rem  Usage:
rem    build-windows.bat                 out: dist\RDOnline + dist\package
rem    build-windows.bat "C:\Games\Rhythm Doctor"   ... and install into that game
rem
rem  What you need:
rem    - Unity 2022.3.62f3c1 with the Windows Build Support (Mono) module
rem    - nothing else: the BepInEx 5 win_x64 loader ships with this repo in
rem      tools\bepinex5-win-x64 (winhttp.dll, doorstop_config.ini, BepInEx\...)
rem
rem  Output layout is checked against the runtime code; see
rem  BUILDING-WINDOWS.md before changing anything here.
rem ====================================================================

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"

set "PROJECT=%ROOT%"
set "OUT=%ROOT%\dist\RDOnline"
set "PACKAGE=%ROOT%\dist\package"
set "BEPINEX=%ROOT%\tools\bepinex5-win-x64"
set "GAME=%~1"
set "LOG=%ROOT%\dist\build.log"
set "UNITY_VERSION=2022.3.62f3c1"

if not exist "%ROOT%\dist" mkdir "%ROOT%\dist"

echo [RDOL] project   : %PROJECT%
echo [RDOL] plugin dir: %OUT%
echo [RDOL] package   : %PACKAGE%
if not "%GAME%"=="" echo [RDOL] game dir  : %GAME%

rem ---------- 1. find Unity ----------
set "UNITY="
if not "%RDOL_UNITY%"=="" (
  if exist "%RDOL_UNITY%" set "UNITY=%RDOL_UNITY%"
)
if "%UNITY%"=="" (
  for %%V in ("%UNITY_VERSION%") do (
    for %%E in (2022.3.62f3c1 2022.3.62f1 2022.3.60f1) do (
      if exist "%ProgramFiles%\Unity\Hub\Editor\%%~E\Editor\Unity.exe" set "UNITY=%ProgramFiles%\Unity\Hub\Editor\%%~E\Editor\Unity.exe"
      if "!UNITY!"=="" if exist "%ProgramFiles(x86)%\Unity\Hub\Editor\%%~E\Editor\Unity.exe" set "UNITY=%ProgramFiles(x86)%\Unity\Hub\Editor\%%~E\Editor\Unity.exe"
    )
  )
)
if "%UNITY%"=="" (
  echo [RDOL] ERROR: Unity editor not found.
  echo [RDOL] Set RDOL_UNITY to Unity.exe, for example:
  echo [RDOL   set RDOL_UNITY=C:\Program Files\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe
  goto :fail
)
if not exist "%UNITY%" (
  echo [RDOL] ERROR: RDOL_UNITY points to a missing file: %UNITY%
  goto :fail
)
echo [RDOL] unity     : %UNITY%

rem ---------- 2. check BepInEx ----------
if not exist "%BEPINEX%\winhttp.dll" (
  echo [RDOL] ERROR: BepInEx 5 loader missing at %BEPINEX%\winhttp.dll
  echo [RDOL] It ships with the repo. Re-clone, or restore that folder from git:
  echo [RDOL] git checkout -- tools/bepinex5-win-x64
  goto :fail
)

rem ---------- 3. build ----------
echo [RDOL] running Unity in batchmode, this takes a few minutes...
"%UNITY%" -batchmode -nographics -projectPath "%PROJECT%" -executeMethod RDOL.Editor.ModBuilder.BuildFromCommandLine -logFile "%LOG%" -rdolOut "%OUT%" -rdolPackage "%PACKAGE%" -rdolBepInEx "%BEPINEX%" -rdolGame "%GAME%"
set "CODE=%ERRORLEVEL%"

if not "%CODE%"=="0" (
  echo [RDOL] ERROR: Unity exited with code %CODE%
  echo [RDOL] Last errors:
  findstr /C:"[RDOL]" /C:"error CS" /C:"Exception" "%LOG%" 2>nul | more +0
  echo [RDOL] Full log: %LOG%
  goto :fail
)

rem ---------- 4. verify the files the runtime actually loads ----------
set "MISSING="
call :need "%OUT%\CacheAssets\rdol.scenes.assets"
call :need "%OUT%\CacheAssets\rdol.resources.assets"
call :need "%OUT%\checkupdate.scene.assets"
call :need "%OUT%\RDOL.dll"
call :need "%OUT%\RDOL.Entry.dll"
call :need "%OUT%\CheckUpdate.dll"
call :need "%OUT%\rdol.config.json"
if not "%MISSING%"=="" (
  echo [RDOL] ERROR: build finished but these are missing:%MISSING%
  goto :fail
)

echo.
echo [RDOL] OK
echo [RDOL] plugin folder : %OUT%
if exist "%PACKAGE%\winhttp.dll" echo [RDOL] installable   : %PACKAGE%  ^(copy its contents into the game folder^)
if not "%GAME%"=="" echo [RDOL] installed     : %GAME%
echo [RDOL] log           : %LOG%
endlocal
exit /b 0

:need
if not exist %1 set "MISSING=%MISSING% %1"
goto :eof

:fail
endlocal
exit /b 1
