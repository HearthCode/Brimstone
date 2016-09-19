@echo off
set PROJECTDIR=%~1
if not exist "%PROJECTDIR%"Games\Hearthstone\Data\CardDefs.xml (
	if not exist "%PROJECTDIR%"Games\Hearthstone\Data (
		mkdir "%PROJECTDIR%"Games\Hearthstone\Data
	)
	git clone https://github.com/HearthSim/hs-data
	copy /y hs-data\CardDefs.xml "%PROJECTDIR%"Games\Hearthstone\Data
	copy /y hs-data\DBF\CARD.xml "%PROJECTDIR%"Games\Hearthstone\Data
	for /d %%p in ("hs-data") do rmdir "%%p" /s /q
)
