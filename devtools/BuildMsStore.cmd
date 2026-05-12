@echo off
rem
rem	Perform a clean restore and build.
rem	Run all unit tests.
rem	Publish a standalone application.
rem	Create an app bundle for the Microsoft Store.
rem

setlocal EnableExtensions EnableDelayedExpansion

if NOT EXIST "ScreenSleeper.slnx" (
  echo Run this script from the solution folder.
  goto :EOF
)

echo.
echo To avoid errors on locked files and folders, such as PackageLayout, etc.:
echo    - Pause Onedrive
echo    - Exit Visual Studio
echo.
echo Update the version in:
echo    - Directory.Build.props

set PROJECT=ScreenSleeper
set ARCHIVE_NAME=%PROJECT%
set BUILD_OUTPUT_ROOT=C:\VSIntermediate\%PROJECT%
set ARTIFACTS_PATH=%BUILD_OUTPUT_ROOT%\artifacts
set PUBLISH_FILES_PATH=%BUILD_OUTPUT_ROOT%\publish
set DIRECTORY_BUILD_PROPS=.\Directory.Build.props
set PROJECT_APP_PATH=.\%PROJECT%\%PROJECT%.csproj
set TARGET_EXE_PATH=%ARTIFACTS_PATH%\bin\%PROJECT%\release_win-x64\%PROJECT%.exe
set PROJECT_TESTS_PATH=.\%PROJECT%.UnitTests\%PROJECT%.UnitTests.csproj

::
:: PRE-BUILD CHECKS
::

echo.
echo === COMPARE VERSIONS IN Directory.Build.props ===
echo.

rem === 1. Locate the Directory.Build.props file ===
if not exist "%DIRECTORY_BUILD_PROPS%" (
	echo ERROR: Version file not found: %DIRECTORY_BUILD_PROPS%
	exit /b 1
)

rem === 2. Extract VersionPrefix from Directory.Build.props ===
for /f "usebackq delims=" %%V in (`
	powershell -NoLogo -NoProfile -Command ^
		"[xml]$x = Get-Content '%DIRECTORY_BUILD_PROPS%'; $x.Project.PropertyGroup.VersionPrefix"
`) do set "VERSION_PREFIX=%%V"

if "%VERSION_PREFIX%" == "" (
	echo ERROR: VersionPrefix not found in %DIRECTORY_BUILD_PROPS%
	exit /b 1
)

echo Version: %VERSION_PREFIX%

set VERSION=%VERSION_PREFIX%

echo.
choice /c YN /n /m "Press N to quit, Y to continue: "
if errorlevel 2 (
	echo Quitting...
	exit /b 0
)

::
:: BUILD
::

echo.
echo === DOTNET CLEAN ===
dotnet clean "%PROJECT_APP_PATH%" --runtime win-x64
if errorlevel 1 exit /b %ERRORLEVEL%
dotnet clean "%PROJECT_TESTS_PATH%"
if errorlevel 1 exit /b %ERRORLEVEL%

echo.
echo === DOTNET RESTORE ===
dotnet restore "%PROJECT_APP_PATH%" --runtime win-x64
if errorlevel 1 exit /b %ERRORLEVEL%
dotnet restore "%PROJECT_TESTS_PATH%"
if errorlevel 1 exit /b %ERRORLEVEL%

echo.
echo === DOTNET BUILD RELEASE ===
dotnet build ^
	"%PROJECT_APP_PATH%" ^
	--configuration Release ^
	--no-restore

if errorlevel 1 exit /b %ERRORLEVEL%

echo.
echo === VERIFY TARGET PROPERTIES ===
if exist "%TARGET_EXE_PATH%" (
	sigcheck.exe -nobanner "%TARGET_EXE_PATH%"
) else (
	echo File does not exist: "%TARGET_EXE_PATH%"
	exit /b
)

::
:: TESTS
::

echo.
echo === DOTNET BUILD UNIT TESTS ===
dotnet build ^
	"%PROJECT_TESTS_PATH%" ^
	--configuration Release ^
	--no-restore

if errorlevel 1 exit /b %ERRORLEVEL%

echo.
echo === DOTNET TEST ===
dotnet test ^
	--project "%PROJECT_TESTS_PATH%" ^
	--configuration Release ^
	--no-restore ^
	--no-build ^
	--no-ansi ^
	--no-progress ^
	--output detailed

if errorlevel 1 exit /b %ERRORLEVEL%

::
:: PUBLISH
::

echo.
echo === DOTNET PUBLISH (Standalone) ===
dotnet publish ^
	"%PROJECT_APP_PATH%" ^
	--configuration Release ^
	--framework=net10.0-windows10.0.26100.0 ^
	--no-restore ^
	--property:Platform=x64 ^
	--property:RuntimeIdentifier=win-x64 ^
	--property:PublishProtocol=FileSystem ^
	--property:SelfContained=true ^
	--property:PublishReadyToRun=false ^
	--property:PublishTrimmed=false ^
	--property:PublishSingleFile=false ^
	--property:PublishDir="%PUBLISH_FILES_PATH%"

if errorlevel 1 exit /b %ERRORLEVEL%

pushd "%PUBLISH_FILES_PATH%"
nanazipc.exe u -tzip "%BUILD_OUTPUT_ROOT%\%ARCHIVE_NAME%_%VERSION%.zip" *.*
popd

echo Publish successful.

endlocal
