@echo off
set PROJECTDIR=%~1
if not exist "%PROJECTDIR%"Games\Hearthstone\Data\Cards.xml (
	if not exist "%PROJECTDIR%"Games\Hearthstone\Data (
		mkdir "%PROJECTDIR%"Games\Hearthstone\Data
	)
	git clone https://gitlab.com/HearthCode/HearthData
	pushd HearthData
	dotnet restore
	dotnet run --extract-card-data="%PROJECTDIR%"Games\Hearthstone\Data\Cards.xml
	popd
	for /d %%p in ("HearthData") do rmdir "%%p" /s /q
)
if not exist "%PROJECTDIR%"Games\Hearthstone\Data\Cards.xml (
	echo HearthData: error %ERRORLEVEL%: Could not generate card metadata XML
	exit /B 1
)
