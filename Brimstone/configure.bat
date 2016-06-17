@echo off
set PROJECTDIR=%~1
if not exist "%PROJECTDIR%"Data\CardDefs.xml (
	if not exist "%PROJECTDIR%"Data (
		mkdir "%PROJECTDIR%"Data
	)
	git clone https://github.com/HearthSim/hs-data
	copy /y hs-data\CardDefs.xml "%PROJECTDIR%"Data
	for /d %%p in ("hs-data") do rmdir "%%p" /s /q
)
